# Cửa sổ tìm kiếm

[← RiseOn.Utils](../../README.md)

Hai cửa sổ tìm kiếm dạng cây, gọi được từ drawer hay editor window tự viết. Viết
bằng UI Toolkit: danh sách chỉ dựng những dòng đang hiện, nên vài nghìn mục vẫn
cuộn mượt. Namespace `RiseOn.Utils.Editor.SearchWindow`.

```csharp
using RiseOn.Utils.Editor.SearchWindow;

// Chọn một kiểu component, cùng danh sách và thư mục với Add Component của Unity.
if (GUI.Button(buttonRect, "Add")) {
    ComponentSearchWindow.Open(buttonRect, type => Debug.Log(type), searchText: "Collider");
}

// Chọn một object: tab Scene và tab Assets, lọc theo kiểu.
if (GUI.Button(buttonRect, "Pick")) {
    ObjectSearchWindow.Open(buttonRect, "Material",
        node => Debug.Log(node.Data as Material),
        filterType: typeof(Material),
        allowSceneObjects: false);
}
```

| Hàm | Tham số chính |
|---|---|
| `ComponentSearchWindow.Open(rect, onSelected, searchText)` | `onSelected` nhận `Type` được chọn. Chỉ liệt kê thứ Add Component cho gắn: component native có trong menu Component (không có lớp cha như `Collider`, không có `Transform`, không có component của module đang tắt), script có file cùng tên lớp. Thư mục của component native lấy từ menu Component, của script lấy từ `AddComponentMenu` hoặc namespace |
| `ObjectSearchWindow.Open(rect, title, onSelected, filterType, rootPrefab, allowSceneObjects, current)` | `onSelected` nhận `SearchNode`, object nằm trong `node.Data`. `filterType` có thể là interface hay generic definition. `rootPrefab`: tìm trong prefab đang mở thay vì scene. `current`: giá trị field đang giữ, cửa sổ mở ra là tô sẵn nó |

`rect` là vùng của nút bấm, tính trong `OnGUI` như `GUILayoutUtility.GetLastRect()`.
Cửa sổ mở ngay dưới nút, hoặc phía trên khi bên dưới không đủ chỗ. Nó là aux window
như cửa sổ Select Object của Unity, nên có thanh tiêu đề, dời và đổi cỡ được.

## Dùng cửa sổ

- Gõ để tìm trên cả cây, mọi cấp. Các từ cách nhau bằng dấu cách, mục phải khớp đủ
  mọi từ, không phân biệt hoa thường. Kết quả chỉ gồm mục chọn được, không có thư mục.
- Tìm theo kiểu mờ của Unity Search (`FuzzySearch`): chữ của từ gõ chỉ cần xuất hiện
  đúng thứ tự, không cần liền nhau, nên `pb` ra `PickableBehaviour`. Kết quả xếp theo
  điểm: khớp ở tên xếp trên khớp ở đường dẫn; trong số đó, tên có một từ trùng hẳn
  từ gõ xếp trước, điểm bằng nhau thì tên ngắn hơn trước. Dòng đầu được tô sẵn.
- `ObjectSearchWindow` mở ra là tô sẵn giá trị field đang giữ: mở đúng tab, mở các
  thư mục bên trên và cuộn tới nó. Field trống thì tô dòng None. Tab lần trước được
  nhớ theo từng kiểu lọc.
- Di chuột để chọn dòng, bấm để lấy. Bấm vào thư mục để mở nó.
- ↑ ↓ chọn dòng, Enter lấy mục hoặc mở thư mục, → mở thư mục, ← hoặc Backspace
  (khi ô tìm đang trống) quay ra, Esc đóng.
- Kéo thanh tiêu đề để dời, kéo mép để đổi cỡ. Cỡ được nhớ theo từng loại cửa sổ,
  lần mở sau giữ nguyên. Bấm ra ngoài thì cửa sổ đóng.

## Tốc độ

- Tab Assets của `ObjectSearchWindow` nạp mọi prefab để xem component trên gốc lẫn
  object con (kể cả đang tắt). Lần đầu, khi prefab chưa nạp, mất chừng nửa mili giây
  mỗi prefab. Kết quả được giữ
  theo kiểu lọc tới khi có asset được import, xóa hay đổi chỗ, nên mở lại gần như
  tức thì.
- Cây của `ComponentSearchWindow` chỉ dựng một lần sau mỗi lần domain reload.
- Icon chỉ nạp khi dòng hiện ra trên màn hình.

## Tự làm cửa sổ khác

Kế thừa `SearchWindow`, khai các tab trong `RegisterSections` bằng
`AddSection(tên, builder)`. `builder` nhận `SectionBuildContext`, dựng cây
`SearchNode`, báo tiến độ bằng `ctx.ReportProgress` rồi gọi `ctx.Complete(root)`.
Mở cửa sổ bằng `Show(rect, title, onSelected, defaultSize, searchText)` từ một hàm
`Open` tĩnh như hai lớp có sẵn; `defaultSize` là cỡ của lần mở đầu tiên. Gọi
`PreselectOnOpen(data)` trước `Show` để tô sẵn mục có `Data` bằng `data`; ghi đè
`StateKey` để nhớ tab riêng theo từng trường hợp. Giao diện nằm trong
`SearchWindow.uss` cạnh script.

| Trường của `SearchNode` | Ý nghĩa |
|---|---|
| `Label` | Chữ của dòng, nhận rich text |
| `LabelSearch` | Chữ của dòng khi đang tìm, nếu cần nói rõ hơn `Label` |
| `SearchName` | Phần được xếp hạng trước khi tìm, thường là tên và kiểu. Khớp ở phần còn lại của nhãn (đường dẫn, namespace) xếp dưới. Mặc định là `Label` bỏ rich text |
| `Icon` | Icon của dòng |
| `IconLoader` | Hàm trả về icon, chỉ gọi khi dòng hiện ra lần đầu. Dùng thay `Icon` khi icon tốn công nạp |
| `Data` | Thứ được trả về khi chọn. Mục có `Data` mới được đếm trên tab |
| `AddChild`, `AddRangeChildren` | Thêm mục con; mục có con là thư mục |

Tên lấy từ object có thể chứa dấu `<` bị hiểu nhầm là thẻ rich text. Bọc đoạn đó
trong `<noparse>...</noparse>` để hiện đúng như gốc.
