# このリポジトリは [pool-parlor/pool-parlor-table](../../../../pool-parlor/pool-parlor-table) の fork でしたが、元リポジトリが非公開になったため、そのコピーをアップロードしたものです。

見つけた不具合の修正や、思いついた機能追加を行います。  
また、各種ゲームルールの実装に取り組んでいます。

基本部分に関わる修正は当面の間 [fork source 1.0.3](../../releases/tag/1.0.3) をベースにした修正ブランチで行います。

- 修正ブランチ [eijis_fork_103](../../tree/eijis_fork_103)

各種ゲームルールの実装はそれぞれのブランチで行います。

# 元リポジトリが非公開になった経緯と、そのコピーをアップロードする行為についての見解

以下の引用内容から、リポジトリのコピーをアップロードして改変を含めて公開することは問題ないと考えます。  

pool-parlor の discord にて公式にアナウンスされた内容から引用  

2023.12.24 のアナウンスより  
https://discord.com/channels/839395904167346227/839970012999843850/1188319294476521553  
↓ Githubに関する部分をdeepL翻訳したもの
```
個人的な理由でGithubを閉鎖することにした。
元々テーブルを公開したくはなかったのだが、大勢の人たちからのリクエストに押されてしまったというのが正直なところだ。私の興味はPool Parlorとそのコミュニティにあり、レポを維持したり他のワールドのために機能するプレハブを作ることではありません。このため、他の人たちからのリクエストや質問が後を絶たないので、私はこれを完全に削除することが私自身の精神衛生上、最良の選択であると判断しました。

私は12月31日にPool Parlor Github Tableを削除します。

オープンソースの開発を続けたいなら、誰でも自由にソースの自分のレポを作ることができますが、私はこのテーブルの開発を、Pool Parlorコミュニティのための厳格な非公開に戻します。
```

suggestions への投稿に対する回答より  
https://discord.com/channels/839395904167346227/1189016912076750888/1189019807710969926  
↓ Githubに関する部分をdeepL翻訳したもの
```
アナウンスで言ったように、私がGithub上でテーブルのソースを削除した後、人々がそれをどうしようが私は気にしない。他の誰かが簡単に全く同じフォーマットでgitに再アップロードして、VRCの残りのためにオープンソース開発を続けることができる。
私はもうそれをメンテナンスする人間にはなりたくないんです。
```

# 以下↓ fork元オリジナルの [README.md](../../tree/dd277b42c971a79e029eb21f631ac64dff7c567a)

Pool Parlor Table


By:
Harry-T
Metaphira

How to import the table:

https://user-images.githubusercontent.com/37195727/212565155-12d425ef-1047-41ae-b1e4-618a04a13b69.mp4

Note: If your layer is not the 22nd unity layer you will need to set the culling mask in the desktop camera to the BilliardModule layer.
