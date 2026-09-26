# RiseOn.Utils

Bộ tiện ích nền cho game Unity: `Singleton`, `Bounds2D`, extension và lớp utils cho
những việc lặp đi lặp lại (vector, transform, collection, Undo, UnityEvent), hai
attribute cho Odin và vài tool Editor.

Package `com.riseon.utils`. Code runtime nằm trong namespace `RiseOn.Utils`,
code Editor trong `RiseOn.Utils.Editor`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Tổng quan](#tổng-quan)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
  - [Singleton](#singleton)
  - [Utils](#utils)
  - [Sửa dữ liệu trong Editor](#sửa-dữ-liệu-trong-editor)
  - [Tool Editor](#tool-editor)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Attribute Inspector ở nhiều lớp, `EnumLabel`, `ForwardAttributesTo`, drawer và tool Editor |
| `com.unity.modules.uielements` (UI Toolkit) | Tự cài theo `package.json`, có sẵn trong Unity | Cửa sổ tìm kiếm |

UI Toolkit là module có sẵn của Unity, bật mặc định. Odin không có trên UPM nên
không khai được trong `package.json`, phải cài vào project trước; thiếu Odin thì
project báo đúng một lỗi từ `RiseOn.Utils.Requirements`.

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
    "com.riseon.utils": "1.0.4"
  }
}
```

Hoặc chạy `openupm add com.riseon.utils` bằng
[openupm-cli](https://openupm.com/docs/getting-started-cli.html).

**Git URL**: *Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/Core#com.riseon.utils/1.0.4
```

Repo chứa nhiều package nên tag có tiền tố tên package
(`com.riseon.utils/<version>`), còn `?path=/Core` chỉ tới thư mục của package này.

**Thư mục local**, khi đang phát triển pack:
`"com.riseon.utils": "file:D:/path/to/Utils/Core"`.

## Tổng quan

Package gồm hai assembly:

| Assembly | Nội dung |
|---|---|
| `RiseOn.Utils` | `Singleton`, `Bounds2D`, attribute, extension, utils. Chạy cả trong game lẫn Editor |
| `RiseOn.Utils.Editor` | Drawer cho attribute, tool Editor, cửa sổ tìm kiếm. Chỉ có trong Editor |

Nhiều hàm phục vụ việc sửa dữ liệu trong Editor (Undo, dirty, persistent
listener, vẽ gizmo) được đặt ở assembly runtime để gọi thẳng được từ component,
nút Odin hay `OnDrawGizmos` mà không cần `#if UNITY_EDITOR`. Trong bản build,
chúng hoặc bị xóa khỏi lời gọi, hoặc không làm gì.

Hai package cùng repo xây trên nền này:
[Utils UI](../UI/README.md) (UGUI) và
[Utils Renderers](../Renderers/README.md) (SpriteRenderer, thứ tự vẽ 2D).

## Hướng dẫn nhanh

### Singleton

Lớp chỉ có một bản trong game thì kế thừa `Singleton<T>`:

```csharp
using RiseOn.Utils;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
    protected override void OnAwake() {
        // thay cho Awake
    }
}

public class Spawner : MonoBehaviour {
    private void Start() {
        transform.SetPositionXY(Vector2.zero);
        Debug.Log(GameManager.Ins.name);
    }
}
```

Singleton còn truy cập được qua interface (`IAudio.Ins`), có tùy chọn
`DontDestroyOnLoad` và cách xử lý bản trùng. Chi tiết:
[Singleton](Runtime/Singleton/README.md).

### Utils

Lớp tĩnh đuôi `Utils`. Phần lớn là extension, gọi thẳng trên đối tượng; phần còn lại
gọi theo tên lớp:

```csharp
var clip  = clips.RandomInside();                   // phần tử ngẫu nhiên
var flat  = transform.position.With(2, 0);          // bản sao đổi z
var last  = queue.PopBack();                        // bỏ và trả phần tử cuối
transform.SetPositionXY(target);                    // đặt x, y, giữ nguyên z
float k = MathUtils.EaseOutPower(t, 3);             // ease-out lũy thừa bậc 3 (OutCubic)
```

Danh sách đầy đủ: [Utils](Runtime/Utils/README.md).

### Sửa dữ liệu trong Editor

Code sửa object trong Edit mode (nút Odin, `OnValidate`, tool) nên ghi Undo trước
và đánh dấu dirty sau, để `Ctrl+Z` hoạt động và scene / prefab được lưu:

```csharp
target.RecordForUndo();
target.name = "Renamed";
target.MarkDirty();
```

Tạo object, đổi cha, thêm component, gắn persistent listener cho UnityEvent cũng
có bản kèm Undo: xem [Utils](Runtime/Utils/README.md#undo) và
[UnityEvent](Runtime/Utils/README.md#unityevent).

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
| Singleton | `Singleton<T>`, `ISingleton<T>` | [Runtime/Singleton](Runtime/Singleton/README.md) |
| Bounds2D | `Bounds` bản 2D, serialize được | [Runtime/Bounds2D](Runtime/Bounds2D/README.md) |
| Utils | Vector, Transform, Collection, Random, Undo, UnityEvent, Handles, Math, Rich text... | [Runtime/Utils](Runtime/Utils/README.md) |
| EnumLabel | Đặt nhãn phần tử mảng theo enum (Odin) | [Runtime/EnumLabel](Runtime/EnumLabel/README.md) |
| ForwardAttributesTo | Chuyển attribute Odin từ field wrapper xuống field bên trong | [Runtime/ForwardAttributes](Runtime/ForwardAttributes/README.md) |
| Tool Editor | Capture Game View, Find References, Replace Component | [Editor/Tools](Editor/Tools/README.md) |
| Cửa sổ tìm kiếm | `ComponentSearchWindow`, `ObjectSearchWindow` cho drawer riêng | [Editor/SearchWindow](Editor/SearchWindow/README.md) |
| Utils Editor | `PrefabUtils`, `InlineEditorImitator` cho người viết drawer | [Editor/Utils](Editor/Utils/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
