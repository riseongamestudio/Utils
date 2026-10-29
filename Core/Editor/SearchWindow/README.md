# Cửa sổ tìm kiếm

[← RiseOn.Utils](../../README.md)

Ba cửa sổ tìm kiếm dạng cây, gọi được từ drawer hay editor window tự viết. Viết
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
| `ComponentSearchWindow.Open(rect, onSelected, searchText, mode)` | `onSelected` nhận `Type` được chọn. Chỉ liệt kê thứ Add Component cho gắn: component native có trong menu Component (không có lớp cha như `Collider`, không có `Transform`, không có component của module đang tắt), script có file cùng tên lớp. Thư mục của component native lấy từ menu Component, của script lấy từ `AddComponentMenu` hoặc namespace |
| `ObjectSearchWindow.Open(rect, title, onSelected, filterType, rootPrefab, allowSceneObjects, current, mode)` | `onSelected` nhận `SearchNode`, object nằm trong `node.Data`. `filterType` có thể là interface hay generic definition. `rootPrefab`: tìm trong prefab đang mở thay vì scene. `current`: giá trị field đang giữ, cửa sổ mở ra là tô sẵn nó |
| `ChoiceSearchWindow.Open<T>(rect, title, choices, onSelected, onCanceled, mode, selected)` | `choices`: các `SearchChoice<T>(path, value, disabledReason, icon)`; dấu `/` trong `path` tạo thư mục, như `Transform/localScale`; `value` null là dòng None; `icon` nạp khi dòng hiện ra. Thư mục và lựa chọn giữ đúng thứ tự đưa vào. Danh sách chỉ được duyệt khi cửa sổ đã hiện. `onSelected` nhận giá trị của lựa chọn; `onCanceled` chạy khi cửa sổ đóng mà chưa chọn gì (Esc, bấm ra ngoài). Chọn nhiều bước: gọi `Present<T>(title, choices, onSelected)` của cửa sổ trả về, từ trong `onSelected` của bước trước; cửa sổ giữ nguyên chỗ và sang bước sau. `selected`: giá trị đang giữ, được làm nổi, mở sẵn thư mục chứa nó và cuộn tới |
| `ChoiceSearchWindow.OpenToggles<T>(rect, title, choices, isOn, onToggled, mode)` | Chọn nhiều: mỗi lần chọn gọi `onToggled` với lựa chọn đó rồi cửa sổ vẫn mở; lựa chọn có dấu tích khi `isOn` trả về true, hỏi lại sau mỗi lần bật tắt. Dùng cho enum `[Flags]` |

`rect` là vùng của nút bấm, tính trong `OnGUI` như `GUILayoutUtility.GetLastRect()`.
Cửa sổ mở ngay dưới nút, hoặc phía trên khi bên dưới không đủ chỗ, theo một trong hai
kiểu (`SearchWindowMode`):

| Kiểu | Trông thế nào | Dùng bởi |
|---|---|---|
| `Window` | Aux window như cửa sổ Select Object của Unity: có thanh tiêu đề, dời và đổi cỡ được, nhớ cỡ theo từng loại cửa sổ | Mặc định của `ComponentSearchWindow`, `ObjectSearchWindow` |
| `DropDown` | Chỉ có nội dung, viền 1px góc vuông, gắn dưới nút như ô chọn của Odin: dòng tiêu đề ở trên, bỏ khi `title` rỗng (mở dưới một field đã có tên thì không cần); đổi chiều cao thì chạy dần tới cỡ mới, danh sách dựng xong thì hiện mờ dần; rộng bằng nút (không hẹp hơn một mức tối thiểu), cao vừa đủ số dòng đang hiện, không giới hạn chiều cao ngoài màn hình; thanh cuộn chỉ hiện khi danh sách đã dừng phóng mà vẫn còn dòng chưa thấy. Vào thư mục khác thì cao theo thư mục đó; lúc gõ tìm thì cao theo số kết quả, chỉ cạnh xa thanh tìm kiếm co giãn, hết chỗ thì cuộn chứ không đẩy thanh tìm kiếm. Dòng điều khiển (tiêu đề, ô tìm, tab) và danh sách là hai cửa sổ ghép liền: mở thì dòng điều khiển phóng ra từ chiều cao 0 rồi tới danh sách, đóng thì ngược lại. Cạnh dòng điều khiển bám vào lúc mở là cạnh neo, đứng yên suốt chuỗi bước, chỉ cạnh đối diện co giãn; không đủ chỗ thì cạnh neo mới dời, và không dời về khi cửa sổ ngắn lại. Bên dưới nút không chứa hết danh sách thì mở lên trên, đảo cả khối: ô tìm ở dưới, danh sách xếp ngược; không bên nào chứa hết thì đè lên field, mở từ giữa field, cao tới hết màn hình, thứ tự như bên dưới. Bấm lại field đang mở thì chỉ đóng; bấm ra ngoài thì đóng ngay lúc nhấn chuột | Mặc định của `ChoiceSearchWindow` |

Cả ba cửa sổ nhận `mode` để mở theo kiểu kia.

## Viết cửa sổ riêng

Kế thừa `SearchWindow`, viết `RegisterSections` gọi `AddSection(label, builder)` cho mỗi tab,
và mở bằng `Show(rect, title, onSelected, defaultSize, searchText, mode)`. `builder` dựng cây
`SearchNode` rồi gọi `ctx.Complete(root)`, có thể báo tiến độ bằng `ctx.ReportProgress`.
Dựng cây theo đường dẫn bằng `node.AddInFolder("Physics/2D", item)`: thư mục thiếu được tạo,
thứ tự giữ như lúc thêm; `root.SortFoldersFirst()` xếp thư mục lên trước rồi theo tên, như
Add Component. `ShowNext(title, onSelected)` thay nội dung trong cùng cửa sổ cho lần chọn
nhiều bước.

## Dòng bị khóa

Gán `SearchNode.DisabledReason` thì dòng hiện mờ, lý do nằm cạnh tên, bấm hay Enter
đều không chọn, và không tính vào số mục của tab. Dùng cho thứ hợp lệ về kiểu nhưng
không dùng được lúc này, ví dụ một property đã có, hay một khối thiếu phép toán.

## Dùng cửa sổ

- Đổi tab thì danh sách trượt sang, cùng hiệu ứng như khi vào hay ra một thư mục.
- Gõ để tìm trên cả cây, mọi cấp. Các từ cách nhau bằng dấu cách, mục phải khớp đủ
  mọi từ, không phân biệt hoa thường. Một từ khớp được ở tên mục hay ở tên các thư mục bên
  trên nó, nên "counter canvas" ra CanvasGroup trong thư mục Counter; khớp ở tên xếp trên.
- Kết quả ghi mờ đường dẫn thư mục phía trước tên (trừ khi `SearchNode.LabelSearch` ghi khác),
  và tô đậm các chữ của tên đã khớp.
- Thư mục khớp theo tên cũng nằm trong kết quả, có mũi tên; bấm hay Enter thì trượt thẳng vào
  thư mục đó.
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

- Danh sách dựng ngay khi mở. Dựng xong trong vòng 250 ms thì cửa sổ hiện thẳng danh sách, không
  qua màn chờ, mở đúng cỡ. Lâu hơn thì màn chờ mới hiện, và giữ ít nhất 400 ms cho khỏi chớp; xong
  thì danh sách hiện mờ dần và cửa sổ chạy dần tới cỡ thật.
- Danh sách kiểu cho bộ chọn generic được dựng ở luồng nền sau mỗi lần biên dịch lại.
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
