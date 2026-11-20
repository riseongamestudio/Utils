# RiseOn.Utils

Bộ tiện ích nền cho game Unity: `Singleton`, `Bounds2D`, extension và lớp utils cho
những việc lặp đi lặp lại (vector, transform, collection, Undo, UnityEvent), ba
attribute cho Odin và vài tool Editor.

Package `com.riseon.utils`, namespace `RiseOn.Utils`.

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
| `com.unity.modules.uielements` 1.0.0 (UI Toolkit) | Tự cài theo `package.json`, có sẵn trong Unity | Cửa sổ tìm kiếm |

Odin không có trên UPM nên phải cài vào project trước. Thiếu Odin thì project báo một
lỗi từ `RiseOn.Utils.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.utils": "1.0.18"
  }
}
```

**Git URL**: *Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Utils.git?path=/Core
```

**Thư mục local**: `"com.riseon.utils": "file:D:/path/to/Utils/Core"`.

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
| *Replace Type* (chuột phải field `[SerializeReference]`) | Đổi giá trị sang kiểu khác, kể cả lớp generic, giữ phần dữ liệu chung |

Cách dùng từng tool: [Tool trong Editor](Editor/Tools/README.md).

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| Singleton | `Singleton<T>`, `ISingleton<T>` | [Runtime/Singleton](Runtime/Singleton/README.md) |
| Bounds2D | `Bounds` bản 2D, serialize được | [Runtime/Bounds2D](Runtime/Bounds2D/README.md) |
| DirectionExt | Một hướng có sẵn hoặc một góc tự chọn, xoay bằng mũi tên trong Inspector (Odin) | [Runtime/DirectionExt](Runtime/DirectionExt/README.md) |
| Utils | Vector, Transform, Collection, Random, Undo, UnityEvent, Handles, Gizmos, Math, Rich text... | [Runtime/Utils](Runtime/Utils/README.md) |
| EnumLabel | Đặt nhãn phần tử mảng theo enum (Odin) | [Runtime/EnumLabel](Runtime/EnumLabel/README.md) |
| ForwardAttributesTo | Chuyển attribute Odin từ field wrapper xuống field bên trong | [Runtime/ForwardAttributes](Runtime/ForwardAttributes/README.md) |
| AllowGenerics | Cho field `[SerializeReference]` chọn được lớp generic, hỏi từng tham số | [Runtime/AllowGenerics](Runtime/AllowGenerics/README.md) |
| SectionGroup, SectionList, SectionReference, ScriptField | `[SectionGroup]` gom member thành một mục của trang settings, `[SectionList]` vẽ một list thành mục có +, × và kéo đổi chỗ, `[SectionReference]` vẽ một `[SerializeReference]` Editor tự chọn lớp thành mục có script trên thanh; `[ScriptField]` hiện một `Type` như ô Script của component | [Editor/Utils](Editor/Utils/README.md#attribute-trong-lớp-dữ-liệu) |
| Tool Editor | Capture Game View, Find References, Replace Component, Replace Type | [Editor/Tools](Editor/Tools/README.md) |
| Foldout đệ quy | Alt+click foldout bất kỳ trong Inspector của Odin mở hay gập cả nhánh bên dưới | [Editor/Utils](Editor/Utils/README.md#foldout-đệ-quy) |
| Cửa sổ tìm kiếm | `ComponentSearchWindow`, `ObjectSearchWindow`, `ChoiceSearchWindow` cho drawer riêng, dạng cửa sổ hoặc drop-down | [Editor/SearchWindow](Editor/SearchWindow/README.md) |
| Chọn giá trị | Mọi ô enum, của Odin lẫn Unity, và mọi ô `[ValueDropdown]` của một giá trị mở drop-down `ChoiceSearchWindow`; `ChoiceFieldUtils` cho ô vẽ tay | [Editor/Utils](Editor/Utils/README.md#chọn-giá-trị-choicefieldutils) |
| Utils Editor | `PrefabUtils`, `InspectorPropertyUtils`, `RectFieldUtils`, `InlineEditorImitator` cho người viết drawer; `SettingsPage`, `SettingsTree`, `SettingsAssetUtils`, `SettingsAssetInspector`, `Section`, `SectionRows`, `SectionTabs`, các ô dùng chung (`PlatformFieldUtils`, `HelpBoxUtils`, `ScriptUtils`, `SpinnerUtils`), `SettingsIssue`, `SettingsFocus`, `PackAdapters`, `PackageRow`, `PackageJobs`, `DeferredSave`, `AssetFindUtils` cho trang settings và inspector; `PixelUtils`, `HoverUtils` cho control vẽ tay | [Editor/Utils](Editor/Utils/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
