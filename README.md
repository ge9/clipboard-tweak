起動中、Ctrl+Alt+Shift+[何かのキー]を押すと、選択されたコンテンツに関して以下の操作を行う。
- Cを押したとき
  - 選択されたコンテンツをソフトウェア内部のスペアのクリップボードに格納する。コピーに時間がかかる場合があるため、完了まで画面中央にウインドウが表示される。
- Xを押したとき
  - Cの場合と同じだが、選択されたコンテンツが切りとられる（Ctrl+Cに対するCtrl+Xと同じ）
- Vを押したとき
  - スペアのクリップボードの内容を貼り付ける。
- Yを押したとき
  - 選択された文字列の全ての文字を、それぞれUnicodeで+0x60000したものに変える。例えば0x20（半角スペース）であれば0x60020になる。例外的に、U+Exxxxの文字については0x10000を足す。
  - 選択された文字列に既にU+6xxxxやU+Fxxxxなどの文字が含まれていれば、逆向きの変換を行う。
  - これを https://github.com/ge9/trivial-font と併用することにより、表示幅を大きく減らした状態でテキストデータを保持することができ、疑似的な折りたたみ機能が実現できる。
- Pを押したとき
  - 選択文字列を引数としてzot.bat（exeのカレントディレクトリから見えるパスにある必要がある）を起動する。該当引用キーの論文をZoteroで開くことを想定。