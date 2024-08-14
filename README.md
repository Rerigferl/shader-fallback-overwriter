# Shader Fallback Overwriter
シェーダーのフォールバック先を非破壊かつ楽に変更できるNDMFプラグイン

![image](https://github.com/user-attachments/assets/cc230a9d-fae4-4fa5-a9f9-61321761d6f9)

## Installation
- [VPM](https://rerigferl.github.io/vpm)
- [UnityPackage](https://github.com/Rerigferl/shader-fallback-overwriter/releases/latest)

## Usage
基本的な使い方は[MA Mesh Settings](https://modular-avatar.nadena.dev/ja/docs/reference/mesh-settings)と同じように、  
`SFO Shader Fallback Settings`コンポーネントを付与したオブジェクトと、その配下に対して設定が適用されます。

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
各プロパティに継承設定が存在し、上位の設定を引き継ぐか、または上書きするかを設定できます。

> Rendering ModeとFacingは、Shader TypeがUnlitまたはToonの時にのみ有効になります。

----

#### `Material List`
特定のマテリアルにのみ設定を適用、または特定のマテリアルを設定から除外することができます。
