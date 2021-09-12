# GuardLayerImage

チュートリアルなどで「特定のボタン以外は半透明の黒のレイヤーで覆う」際に便利なスクリプトおよびシェーダー。

<img width="345" alt="ss1" src="https://user-images.githubusercontent.com/1532322/132948672-c3ee7cdb-db29-48bf-8fe4-95aa67b76398.png">

## 動作条件

- Unity 2019.4.0f1 以降

## 使い方

1. `Canvas` の前面のほう（ヒエラルキーの下のほう）に `GuardLayerImage` をアタッチした `GameObject` を置く。
2. `GuardLayerImage.SetTargetObject(GameObject object)` に表示したいオブジェクトを設定する。
    - 対象オブジェクトが `Image` なら、設定されている `Sprite` で切り抜かれる。
    - 対象オブジェクトが `Image` でないなら、`RectTransform` の形で切り抜かれる。
3. `GuardLayerImage` の `margin`（対象オブジェクトの周囲の隙間）をいい感じに調整する。

## サンプル

- SampleScene を実行する
