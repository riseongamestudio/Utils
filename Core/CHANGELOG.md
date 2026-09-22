# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.0] - Chưa phát hành

### Thêm

- `Singleton<T>` / `ISingleton<T>`, truy cập được qua lớp hoặc qua interface.
- `Bounds2D`, `VecAxis`, `WaitForSecondCache`.
- Extension cho Vector, Transform, Color, Collection, Random, Object, Coroutine,
  String, Enum, DOTween, Undo và UnityEvent.
- Lớp tiện ích `UndoUtils`, `HandlesUtils`, `MathUtils`.
- Attribute `EnumLabel` và `ForwardAttributesTo` (cần Odin Inspector).
- Tool Editor: Capture Game View, Find References In Scene BETTER, Replace
  Component, cửa sổ tìm kiếm component / object, `InlineEditorImitator`,
  `PrefabExtensions`.
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
