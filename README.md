# Shader Fallback Overwriter
シェーダーのフォールバック先を非破壊かつ楽に変更できるNDMFプラグイン
![image](https://github.com/user-attachments/assets/892c276b-7fad-4570-9494-ee518f1836b5)

## Installation
- [VPM](https://rerigferl.github.io/vpm)
- [UnityPackage](https://github.com/Rerigferl/shader-fallback-overwriter/releases/latest)

## Usage
基本的な使い方は[MA Mesh Settings](https://modular-avatar.nadena.dev/ja/docs/reference/mesh-settings)と同じように、
`Shader Fallback Setting`コンポーネントを付与したオブジェクトと、その配下に対して設定が適用されます。

### Configuration

#### `Fallback Overwrite Mode`
|   Mode   | Description |
| :------: | ----------- |
| Inherit  | 上位に存在する設定を引き継ぎます。 |
| Set      | 設定を上書きします。 |
| Coalesce | 上位に設定が存在しない場合に設定を上書きします。 |
| DontSet  | 設定の上書きを無視し、マテリアルの設定を使用します。 |

----

#### `Shader Type`, `Rendering Mode`, `Facing`
フォールバック先のシェーダーを変更できます。詳しくは[公式のドキュメント](https://creators.vrchat.com/avatars/shader-fallback-system/)を。

Rendering ModeとFacingは、Shader TypeがUnlitまたはToonの時にのみ有効になります。

----

#### `Material List`
特定のマテリアルにのみ設定を適用、または特定のマテリアルを設定から除外することができます。
