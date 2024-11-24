//#define TKCH_DEBUG_UNDO

using System;
using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class PracticeManager : UdonSharpBehaviour
{
    private BilliardsModule table;

    private object[] history = new object[128];
    
    private int currentPtr;
    private int latestPtr;

    /* private bool hack_currentlyLoading;
    private bool hack_dontRecordNext; */

    public void _Init(BilliardsModule table_)
    {
        table = table_;

        _Clear();
    }

    public void _Tick()
    {
    }

    public void _Clear()
    {
        Array.Clear(history, 0, history.Length);
        currentPtr = 0;
        latestPtr = 0;
    }

    public void _Record()
    {
#if TKCH_DEBUG_UNDO || TKCH_DEBUG_SCORE
        table._LogInfo("TKCH PracticeManager::_Record()");
#endif
        
        /*if (hack_currentlyLoading) return;
        
        if (hack_dontRecordNext)
        {
            hack_dontRecordNext = false;
            currentPtr++;
            return;
        }*/

        if (!table.isPracticeMode) return; // doesn't matter

        int stateIdLocal = table.networkingManager.stateIdSynced;
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  stateIdLocal = {stateIdLocal}");
#endif
        
        if (stateIdLocal == currentPtr) return; // already seen

        if (stateIdLocal < 0 || stateIdLocal >= 1024) return; // abuse?

        // set current pointer to whatever we're recording
        currentPtr = stateIdLocal;
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  currentPtr = {currentPtr}");
#endif

        // expand if needed
        if (currentPtr >= history.Length)
        {
            int newSize = history.Length * 2;
            if (newSize < currentPtr) newSize = currentPtr;

            object[] newHistory = new object[newSize];
            Array.Copy(history, newHistory, history.Length);
            history = newHistory;
        }

        object oldValue = history[currentPtr];
        object newValue = table._SerializeInMemoryState();
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  oldValue.length = {(ReferenceEquals(null, oldValue) ? 'x' : ((object[])oldValue).Length)} newValue.length = {(ReferenceEquals(null, newValue) ? 'x' : ((object[])newValue).Length)}");
#endif

        history[currentPtr] = newValue;

        // set latest pointer to current pointer if we're diverging from history
        if (oldValue != null && !table._AreInMemoryStatesEqual((object[])oldValue, (object[])newValue))
        {
#if TKCH_DEBUG_UNDO
            table._LogInfo("  latestPtr = currentPtr");
#endif
            latestPtr = currentPtr;
        }
        // otherwise, set it only if we're seeing something new
        else if (stateIdLocal > latestPtr)
        {
#if TKCH_DEBUG_UNDO
            table._LogInfo("  latestPtr = stateIdLocal");
#endif
            latestPtr = stateIdLocal;
        }

#if TKCH_DEBUG_UNDO || TKCH_DEBUG_SCORE
        //table._LogInfo($"  history[currentPtr={currentPtr}][17] = {((uint[])((object[])history[currentPtr])[17])[0]:X8}-{((uint[])((object[])history[currentPtr])[17])[1]:X8}");
        //table._LogInfo($"  history[latestPtr={latestPtr}][17] = {((uint[])((object[])history[latestPtr])[17])[0]:X8}-{((uint[])((object[])history[latestPtr])[17])[1]:X8}");
        dumpUndoHistory();
#endif

        table._LogInfo($"recording state current={currentPtr} latest={latestPtr}");
    }

    public void _Undo()
    {
#if TKCH_DEBUG_UNDO
        table._LogInfo("TKCH PracticeManager::_Undo()");
#endif
        int newPtr = pop();
        if (newPtr == -1)
        {
            table._IndicateError();
            return;
        }
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  newPtr = {newPtr}");
#endif

        load(newPtr);
    }

    public void _Redo()
    {
#if TKCH_DEBUG_UNDO
        table._LogInfo("TKCH PracticeManager::_Redo()");
#endif
        int newPtr = push();
        if (newPtr == -1)
        {
            table._IndicateError();
            return;
        }
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  newPtr = {newPtr}");
#endif

        load(newPtr);
    }

    private int push()
    {
        int newPtr = currentPtr;

        while (newPtr < latestPtr)
        {
            newPtr++;

            if (history[newPtr] == null) continue;

            return newPtr;
        }

        return -1;
    }

    private int pop()
    {
        int newPtr = currentPtr;

        while (newPtr > 0)
        {
            /*if (currentPtr <= 1)
            {
                table._IndicateError();
                return false;
            }*/
            newPtr--;

            if (history[newPtr] == null) continue;

            object[] state = (object[])history[newPtr];
            if ((byte)state[9] == 0 || (byte) state[9] == 2)
            {
                return newPtr;
            }
        }

        return -1;
    }

    private void load(int newPtr)
    {
#if TKCH_DEBUG_UNDO
        table._LogInfo("TKCH PracticeManager::load()");
#endif
        if (table.isLocalSimulationRunning)
        {
            table._LogInfo("interrupting simulation and loading new state");
        }
        
#if TKCH_DEBUG_UNDO
        table._LogInfo($"  newPtr = {newPtr}");
#endif
        
        object[] state = (object[])history[newPtr];
        // hack_dontRecordNext = (byte) state[9] == 1;
        // hack_currentlyLoading = true;
        table._LoadInMemoryState(state, newPtr);
        // hack_currentlyLoading = false;

        table._IndicateSuccess();
    }

#if TKCH_DEBUG_UNDO
    public void dumpUndoHistory()
    {
        table._LogInfo($"TKCH PracticeManager::dumpUndoHistory() length = {history.Length}");
        for (int i = 0; i <= latestPtr; i++)
        {
            if (ReferenceEquals(null, history[i]))
            {
                table._LogInfo($"  {i} null {(i == currentPtr ? 'C' : ' ')} {(i == latestPtr ? 'L' : ' ')}");
            }
            else
            {
                //object[] objects = (object[])history[i];
                //table._LogInfo($"  {i} Length = {objects.Length} {(i == currentPtr ? 'C' : ' ')} {(i == latestPtr ? 'L' : ' ')}");
                table._LogInfo($"  {i} {((object[])history[i])[17]} {((uint[])((object[])history[i])[18])[0]:X8}-{((uint[])((object[])history[i])[18])[1]:X8} {(i == currentPtr ? 'C' : ' ')} {(i == latestPtr ? 'L' : ' ')}");
            }
        }
    }
#endif
}
