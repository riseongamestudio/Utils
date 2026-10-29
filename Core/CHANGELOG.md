# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.12] - 2026-10-29

### Thêm

- `RectFieldUtils` (Editor): vẽ giá trị một `InspectorProperty` trong rect cho sẵn, có dấu
  override của prefab và Undo; enum mở drop-down tìm kiếm, `[Flags]` bật tắt từng cờ; `Split`
  chia cột giá trị làm hai nửa thẳng hàng.
- `DirectionExt`: một hướng (`Left`, `Right`, `Up`, `Down`) hoặc `Custom` với một góc tính
  bằng độ; `ToVector2()` trả về vector độ dài 1. Inspector của Odin vẽ trên một dòng, góc
  `Custom` xoay bằng cách kéo một mũi tên, cạnh ô nhập số độ.
- `[AllowGenerics]`: cho ô chọn kiểu của field `[SerializeReference]` có cả lớp generic
  mà Odin không suy ra được tham số, và dựng kiểu bằng cách chọn từng tham số, lồng sâu
  bao nhiêu cũng được. Gắn ở field, hoặc ở class/interface để áp cho mọi field kiểu đó.
  Kế thừa và viết `Accepts` để lọc tham số, `GetDisabledReason` để làm xám kiểu không
  dùng được kèm lý do. Tham số mà kiểu của field quyết định được suy sẵn, theo đúng vị trí
  trong khai báo và qua variance (`in`, `out`), nên chỉ hỏi phần còn thiếu.
  Mọi bước chọn nằm trong một `ChoiceSearchWindow` dạng drop-down, chuyển bước không đóng
  cửa sổ; kiểu xếp thư mục theo `[TypeRegistryItem]` như Odin, còn lại theo namespace.
- Cửa sổ tìm kiếm: `ChoiceSearchWindow` chọn một trong các `SearchChoice<T>` dựng bằng code,
  thư mục theo dấu `/` trong đường dẫn; `SearchNode.AddInFolder` và `SortFoldersFirst` dựng và
  xếp cây theo đường dẫn; mọi cửa sổ cụ thể nhận `mode`; `SearchWindowMode.DropDown` mở cửa sổ dạng drop-down gắn dưới
  nút, rộng bằng nút, cao vừa số dòng, có dòng tiêu đề; `SearchNode.DisabledReason` làm mờ một
  dòng kèm lý do và chặn chọn nó; lớp con gọi `ShowNext` để sang bước sau trong cùng cửa sổ.
  Tìm kiếm khớp cả tên thư mục bên trên, hiện đường dẫn và tô đậm chữ khớp trong kết quả, có cả
  thư mục để trượt vào; drop-down đổi chiều cao mượt và hiện danh sách mờ dần; đổi tab có hiệu ứng
  trượt; màn chờ chỉ hiện khi dựng danh sách quá 250 ms; `onCanceled` khi đóng mà chưa chọn.
  Drop-down cuộn xuống từ dưới nút khi mở và cuộn lên khi đóng; `SearchChoice.FolderIcon` cho thư
  mục chứa choice một icon riêng (như icon GameObject), `AddInFolder` trả về thư mục đã thêm vào.
  Bấm lại đúng nút đã mở drop-down thì chỉ đóng nó, không mở lại và không tính là hủy.
  `GenericTypePicker.Pick` nhận tiêu đề cửa sổ, như tên field; `ChoiceSearchWindow.Open` nhận giá trị
  đang chọn để làm nổi và cuộn tới khi mở; `ChoiceSearchWindow.OpenToggles` chọn nhiều, mỗi lần chọn bật
  tắt một lựa chọn có dấu tích, cửa sổ vẫn mở.
  Drop-down là hai cửa sổ ghép liền: dòng điều khiển (tiêu đề, ô tìm, tab) và danh sách. Cạnh
  dòng điều khiển bám vào lúc mở đứng yên suốt chuỗi bước, chỉ cạnh đối diện co giãn, hết chỗ
  mới dời; không đủ chỗ dưới nút thì mở lên trên và đảo cả khối (ô tìm ở dưới, danh sách xếp
  ngược), không bên nào đủ thì đè lên field. Cao vừa số dòng tới hết màn hình, gõ tìm thì co
  giãn theo số kết quả; thanh cuộn chỉ hiện khi danh sách đã dừng mà vẫn còn dòng. Mở thì dòng
  điều khiển rồi danh sách phóng ra, đóng thì ngược lại; nhấn chuột ra ngoài hay editor mất focus
  thì đóng ngay lúc nhấn. `title` rỗng thì không có dòng tiêu đề; cột icon chỉ có khi có dòng
  mang icon (`SearchNode.HasIcon`). `SearchChoice<T>.None()` thêm dòng chọn giá trị trống, tên
  "Null" khi kiểu nhận null, "Default" khi không.
- Alt+click foldout bất kỳ trong Inspector của Odin mở hay gập cả nhánh bên dưới
  (`RecursiveFoldoutDrawer`); `InspectorPropertyUtils.SetExpandedRecursively` cho drawer tự làm.
- *Replace Type* (chuột phải field `[SerializeReference]`, giá trị `SerRef` hay phần tử
  `ListSerRef`): đổi giá trị sang kiểu khác, giữ field cùng tên và giá trị con
  `[SerializeReference]`, trỏ các field dùng chung sang giá trị mới. Danh sách kiểu như ô chọn
  của Odin, có thêm lớp generic khi field có `AllowGenerics`. `[HideReplaceType]` bỏ mục này ở
  field hay kiểu không đổi được bằng cách chép field.

## [1.0.11] - 2026-10-23

### Thêm

- `CollectionUtils.TryIndexOf`: `IndexOf` theo dạng `Try`, không tìm thấy thì trả `false`
  thay vì `-1`: `if (list.TryIndexOf(item, out var index))`.

## [1.0.10] - 2026-10-21

### Đổi

- Các hàm của `LogUtils` thêm tiền tố `With`: `WithLog`, `WithLogWarning`, `WithLogError`,
  `WithLogAssertion`, `WithLogException` và các bản `With…Format`, đọc thành câu:
  `return false.WithLogError("...")`.
- `Singleton` chỉ ném exception khi kiểu đăng ký không khớp, không log thêm lỗi trùng.

## [1.0.9] - 2026-10-20

### Đổi

- `ValueUtils.Exchange` và `StructUtils.Exchange` trả giá trị cũ thay vì đưa ra qua `out`, như
  `std::exchange` của C++, nên dùng được trong biểu thức: `current.Exchange(next).Dispose()`.

## [1.0.8] - 2026-10-20

### Thêm

- `LogUtils`: bộ hàm `Debug.Log` (`Log`, `LogWarning`, `LogError`, `LogAssertion`, `LogException`
  và các bản `Format`) viết thành extension trên giá trị cần trả về. Mỗi hàm log như hàm cùng tên
  của `Debug` rồi trả lại chính giá trị đó, để câu thoát sớm gọn trong một dòng:
  `return false.LogError("Config is not loaded.");`.
- `ValueUtils.Exchange(ref target, value, out previous)`: gán giá trị mới và trả giá trị cũ qua
  `out`, dùng cho mọi kiểu.
- `StructUtils`: extension sửa thẳng biến struct, có `Exchange` gọi trên biến
  (`state.Exchange(next, out var previous)`).

### Đổi

- `RaiseTo` / `LowerTo` chuyển từ `ValueUtils` sang `StructUtils`. Lời gọi dạng extension
  (`score.RaiseTo(next)`) không phải sửa; lời gọi dạng tĩnh `ValueUtils.RaiseTo(ref x, y)` đổi thành
  `StructUtils.RaiseTo(ref x, y)`.

## [1.0.7] - 2026-10-14

### Đổi

- Tên hiển thị trong Package Manager thành `RiseOn Utils`, để khỏi lẫn với package khác cũng tên `Utils`.

## [1.0.6] - 2026-09-28

### Đổi

- `HandleUtils.GetMatrix` / `SetMatrix` gộp thành property `HandleUtils.matrix`, như `Handles.matrix`.
- `HandleUtils.DrawRect` đưa hai màu ra sau bốn góc: `DrawRect(c0, c1, c2, c3, faceColor, outlineColor)`.
- Độ dày mặc định của các đường `HandleUtils` (`width`, `outlineWidth`) từ `1` lên `3`.
- Đường `HandleUtils` bù lại phần alpha mà `Handles.DrawAAPolyLine` tự giảm (còn 75%), nên màu ra
  đúng màu truyền vào.
- `GizmoUtils`: khớp gắt hơn 120° thì viền mặt ngoài vát thay vì kéo mũi nhọn ra xa nét.

### Thêm

- `HandleUtils.DrawLine`, `DrawPath`, `DrawWireCircle`, `DrawWireCapsule`: đường khử răng cưa không
  viền, dày theo pixel màn hình; hình tròn và capsule có tùy chọn `cross` vẽ thêm đường chữ thập.
- `HandleUtils.DrawRect` nhận `Vector3[]` bốn góc hoặc `Rect`.

## [1.0.5] - 2026-09-28

### Đổi

- `HandlesUtils` đổi tên thành `HandleUtils` (GUID file giữ nguyên); code gọi tên cũ phải đổi theo.

### Thêm

- `HandleUtils.DrawOutlinedLine`, `DrawOutlinedPath`, `DrawOutlinedWireCircle`: đường vẽ đè lên một
  đường rộng hơn màu viền, độ dày và viền tính bằng pixel màn hình, không cấp phát.
- `GizmoUtils` với cùng ba hàm, vẽ bằng `Gizmos` nên theo `Gizmos.matrix` và nút bật/tắt Gizmos;
  viền 1 pixel mỗi bên, gấp theo góc ở mỗi khớp.

## [1.0.4] - 2026-09-26

### Đổi

- `ToUpperFirst` đổi tên thành `UpcaseFirst`; code gọi tên cũ phải đổi theo.

### Thêm

- `DowncaseFirst`, `UpcaseLast`, `DowncaseLast`: cùng họ với `UpcaseFirst`, đổi hoa / thường ở ký
  tự đầu hoặc cuối theo văn hoá bất biến.

## [1.0.3] - 2026-09-26

### Đổi

- `Singleton.duplicateAction` bỏ `FormerlySerializedAs`: scene và prefab còn lưu tên cũ `onDuplicate`
  hoặc `destroyDuplicateTarget` phải đổi key thành `duplicateAction`, không thì giá trị về mặc định
  (*Destroy Game Object*).

## [1.0.2] - 2026-09-26

### Đổi

- Field `onDuplicate` của `Singleton` đổi tên thành `duplicateAction` (Inspector: *Duplicate Action*).
  Dữ liệu cũ vẫn đọc được nhờ `FormerlySerializedAs`.

## [1.0.1] - 2026-09-25

### Sửa

- Thiếu Odin Inspector, hoặc thiếu define `ODIN_INSPECTOR` ở nền tảng đang chọn, thì project
  chỉ báo một lỗi rõ ràng từ assembly `RiseOn.Utils.Requirements` thay vì hàng loạt lỗi biên dịch.
  Các assembly dùng Odin có thêm `defineConstraints: ODIN_INSPECTOR`.

## [1.0.0] - 2026-09-23

### Thêm

- `Singleton<T>` / `ISingleton<T>`, truy cập được qua lớp hoặc qua interface.
- `Bounds2D`.
- Lớp tiện ích đuôi `Utils`, phần lớn là extension: Vector, Transform, Color,
  Collection (có push / pop / peek hai đầu cho `IList<T>`), Random, Object, String
  (có các hàm `TagXX()` bọc thẻ rich text), Undo, UnityEvent, Handles, Math, Polygon,
  Value (`Swap`, `RaiseTo`, `LowerTo`).
- Attribute `EnumLabel` và `ForwardAttributesTo` (cần Odin Inspector).
- Tool Editor: Capture Game View, Find References In Scene BETTER, Replace
  Component, cửa sổ tìm kiếm component / object, `InlineEditorImitator`,
  `PrefabUtils`.
- Cửa sổ tìm kiếm viết bằng UI Toolkit: danh sách chỉ dựng các dòng đang hiện,
  icon chỉ nạp khi dòng hiện ra (`SearchNode.IconLoader`), cây component dựng một
  lần mỗi lần domain reload, kết quả tab Assets giữ lại tới khi có asset thay đổi.
  Tìm kiếm không còn khớp nhầm vào thẻ rich text như `color`. Mở dạng aux window
  như cửa sổ Select Object của Unity: dời bằng thanh tiêu đề, đổi cỡ bằng mép, nhớ
  cỡ theo từng loại cửa sổ. Style nằm trong `SearchWindow.uss`, màu theo theme Unity.
- Tìm kiếm mờ bằng `FuzzySearch` của Unity Search và xếp hạng kết quả: khớp ở tên
  (`SearchNode.SearchName`) trước khớp ở đường dẫn, từ trùng hẳn trước tiền tố.
- `ObjectSearchWindow` tô sẵn giá trị đang gán (`current`), nhớ tab theo kiểu lọc,
  ghi kiểu trên dòng asset, hiện sub-asset bằng tên của nó, quét cả component con
  trong prefab, và liệt kê GameObject khi lọc `GameObject` hay `Object`.
- `ComponentSearchWindow` chỉ liệt kê thứ Add Component của Unity cho gắn, xếp theo thư mục của
  menu Component.
