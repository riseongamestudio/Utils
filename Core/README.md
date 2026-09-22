# RiseOn.Utils

Bộ tiện ích nền cho game Unity: lớp cơ sở thay cho `MonoBehaviour` /
`ScriptableObject`, `Singleton`, `Bounds2D`, extension và helper cho những việc
lặp đi lặp lại (vector, transform, collection, coroutine, DOTween, Undo,
UnityEvent), hai attribute cho Odin và vài tool Editor.

Package `com.riseon.utils`. Code runtime nằm trong namespace `RiseOn.Utils`,
code Editor trong `RiseOn.Utils.Editor`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Tổng quan](#tổng-quan)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
  - [Lớp nền và Singleton](#lớp-nền-và-singleton)
  - [Extension và helper](#extension-và-helper)
  - [Sửa dữ liệu trong Editor](#sửa-dữ-liệu-trong-editor)
  - [Tool Editor](#tool-editor)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute Inspector ở nhiều lớp, `SerializedScriptableObjectExt`, `EnumLabel`, `ForwardAttributesTo`, drawer và tool Editor |
| [DOTween](https://dotween.demigiant.com) | Cài tay từ Asset Store, rồi chạy *Tools → Demigiant → DOTween Utility Panel → Setup DOTween* | `DOTweenExtensions` |

Package không có phụ thuộc UPM nào. Odin và DOTween không có trên UPM nên không
khai được trong `package.json`, phải cài vào project trước; thiếu một trong hai
thì assembly `RiseOn.Utils` không biên dịch được.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils": "1.0.0"
  }
}
```

Hoặc chạy `openupm add com.riseon.utils` bằng
[openupm-cli](https://openupm.com/docs/getting-started-cli.html).

**Git URL**: *Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/Core#com.riseon.utils/1.0.0
```

Repo chứa nhiều package nên tag có tiền tố tên package
(`com.riseon.utils/<version>`), còn `?path=/Core` chỉ tới thư mục của package này.

**Thư mục local**, khi đang phát triển pack:
`"com.riseon.utils": "file:D:/path/to/Utils/Core"`.

## Tổng quan

Package gồm hai assembly:

| Assembly | Nội dung |
|---|---|
| `RiseOn.Utils` | Lớp nền, `Singleton`, `Bounds2D`, attribute, extension, helper. Chạy cả trong game lẫn Editor |
| `RiseOn.Utils.Editor` | Drawer cho attribute, tool Editor, cửa sổ tìm kiếm. Chỉ có trong Editor |

Nhiều hàm phục vụ việc sửa dữ liệu trong Editor (Undo, dirty, persistent
listener, vẽ gizmo) được đặt ở assembly runtime để gọi thẳng được từ component,
nút Odin hay `OnDrawGizmos` mà không cần `#if UNITY_EDITOR`. Trong bản build,
chúng hoặc bị xóa khỏi lời gọi, hoặc không làm gì.

Hai package cùng repo xây trên nền này:
[Utils UI](../UI/README.md) (UGUI) và
[Utils Renderers](../Renderers/README.md) (SpriteRenderer, thứ tự vẽ 2D).

## Hướng dẫn nhanh

### Lớp nền và Singleton

Kế thừa `MonoBehaviourExt` thay cho `MonoBehaviour` để có `TF` (transform được
cache) và cặp hàm `RecordForUndo` / `MarkDirty`. Lớp chỉ có một bản trong game
thì kế thừa `Singleton<T>`:

```csharp
using RiseOn.Utils;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
    protected override void OnAwake() {
        // thay cho Awake
    }
}

public class Spawner : MonoBehaviourExt {
    private void Start() {
        TF.SetPositionXY(Vector2.zero);
        Debug.Log(GameManager.Ins.name);
    }
}
```

Singleton còn truy cập được qua interface (`IAudio.Ins`), có tùy chọn
`DontDestroyOnLoad` và cách xử lý bản trùng. Chi tiết:
[Singleton](Runtime/Singleton/README.md).

### Extension và helper

Extension gọi thẳng trên đối tượng, helper gọi theo tên lớp:

```csharp
var clip  = clips.RandomInside();                   // phần tử ngẫu nhiên
var flat  = TF.position.With(VecAxis.Z, 0);         // đổi một trục
this.DelayedCall_Second(1f, ShowResult);            // gọi sau 1 giây
TF.DOJump_BetterHeight(target, 1f, 0.5f);           // nhảy với đỉnh và nhịp tự nhiên hơn DOJump
float k = MathHelper.Evaluate(t, 3);                // ease-out bậc 3
```

Danh sách đầy đủ: [Extension](Runtime/Extensions/README.md),
[Helper](Runtime/Helpers/README.md).

### Sửa dữ liệu trong Editor

Code sửa object trong Edit mode (nút Odin, `OnValidate`, tool) nên ghi Undo trước
và đánh dấu dirty sau, để `Ctrl+Z` hoạt động và scene / prefab được lưu:

```csharp
UndoHelper.RecordForUndo(target);   // trong lớp kế thừa MonoBehaviourExt: RecordForUndo(target)
target.name = "Renamed";
UndoHelper.MarkDirty(target);       // hoặc MarkDirty(target)
```

Tạo object, đổi cha, thêm component, gắn persistent listener cho UnityEvent cũng
có bản kèm Undo: xem [Helper](Runtime/Helpers/README.md#undohelper) và
[Extension](Runtime/Extensions/README.md#undo).

### Tool Editor

| Menu | Việc |
|---|---|
| *Tools → RiseOn → Capture Game View* (`Alt+Shift+C`) | Chụp Game view ra PNG (Windows) |
| *Find References In Scene BETTER* (chuột phải component hoặc GameObject) | Tìm mọi component đang tham chiếu tới đối tượng |
| *Replace Component* (chuột phải component) | Đổi component sang kiểu khác, giữ dữ liệu và tham chiếu |

Cách dùng từng tool: [Tool trong Editor](Editor/Tools/README.md).

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| Lớp nền | `MonoBehaviourExt`, `ScriptableObjectExt`, `SerializedScriptableObjectExt` | [Runtime/Bases](Runtime/Bases/README.md) |
| Singleton | `Singleton<T>`, `ISingleton<T>` | [Runtime/Singleton](Runtime/Singleton/README.md) |
| Bounds2D | `Bounds` bản 2D, serialize được | [Runtime/Bounds2D](Runtime/Bounds2D/README.md) |
| Extension | Vector, Transform, Collection, Random, Coroutine, DOTween, Undo, UnityEvent... | [Runtime/Extensions](Runtime/Extensions/README.md) |
| Helper | `UndoHelper`, `HandlesHelper`, `MathHelper`, `WaitForSecondCache` | [Runtime/Helpers](Runtime/Helpers/README.md) |
| EnumLabel | Đặt nhãn phần tử mảng theo enum (Odin) | [Runtime/EnumLabel](Runtime/EnumLabel/README.md) |
| ForwardAttributesTo | Chuyển attribute Odin từ field wrapper xuống field bên trong | [Runtime/ForwardAttributes](Runtime/ForwardAttributes/README.md) |
| Tool Editor | Capture Game View, Find References, Replace Component | [Editor/Tools](Editor/Tools/README.md) |
| Cửa sổ tìm kiếm | `ComponentSearchWindow`, `ObjectSearchWindow` cho drawer riêng | [Editor/SearchWindow](Editor/SearchWindow/README.md) |
| Extension Editor | `PrefabExtensions` | [Editor/Extensions](Editor/Extensions/README.md) |
| Helper Editor | `InlineEditorImitator` cho người viết drawer | [Editor/Helpers](Editor/Helpers/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
