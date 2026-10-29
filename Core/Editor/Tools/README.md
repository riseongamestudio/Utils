# Tool trong Editor

[← RiseOn.Utils](../../README.md)

## Capture Game View

Chụp Game view ra file PNG, đúng độ phân giải đang đặt cho Game view. Chỉ có
trên Windows.

1. Mở Game view.
2. Bấm `Alt+Shift+C`, hoặc chọn *Tools → RiseOn → Capture Game View*.
3. Chọn nơi lưu (mặc định thư mục Downloads).

## Find References In Scene BETTER

Tìm mọi component đang tham chiếu tới một đối tượng.

1. Chuột phải header một component trong Inspector, hoặc chuột phải GameObject
   trong Hierarchy.
2. Chọn *Find References In Scene BETTER*.

- Phạm vi tìm: prefab đang mở nếu đối tượng thuộc prefab đó, còn không thì các
  scene đang mở.
- Console log từng component tìm được kèm đường dẫn property. Hierarchy chọn và
  ping các GameObject đó.

## Replace Component

Đổi một component sang kiểu khác mà không mất dữ liệu và tham chiếu.

1. Chọn một hoặc nhiều GameObject.
2. Chuột phải header component cần đổi, chọn *Replace Component*.
3. Chọn kiểu mới trong cửa sổ tìm kiếm (ô tìm kiếm điền sẵn tên kiểu cũ).

Kết quả:

- Component mới nằm đúng vị trí của component cũ trong danh sách.
- Field cùng tên, cùng kiểu được chép sang.
- Tham chiếu tới component cũ trong scene đang mở, và trong prefab chứa nó,
  được trỏ sang component mới. Tham chiếu từ scene hay prefab khác thì không.
- Một lần `Ctrl+Z` hoàn tác cả thao tác.
- Chọn đúng kiểu cũ thì bỏ qua, có cảnh báo.

## Replace Type

Đổi giá trị của một field `[SerializeReference]` sang kiểu khác mà không mất phần dữ
liệu hai kiểu có chung. Chọn kiểu qua menu thường của Odin thì tạo instance mới, mất
sạch dữ liệu cũ.

1. Chọn một hoặc nhiều object.
2. Chuột phải field cần đổi, chọn *Replace Type*. Có ở field `[SerializeReference]`
   thường, giá trị trong `SerRef<T>`, từng phần tử của `ListSerRef<T>` hay của list có
   `[SerializeReference]`, kể cả field lồng bên trong một giá trị khác. Field đang là
   *None* (mọi object được chọn) thì mục này bị làm mờ vì không có gì để thay; chọn kiểu
   đầu tiên bằng ô chọn của field.
3. Chọn kiểu mới trong [cửa sổ tìm kiếm](../SearchWindow/README.md) dạng drop-down, mở ngay dưới
   ô giá trị của field và rộng bằng nó. Danh
   sách gồm đúng các kiểu ô chọn của Odin đưa ra cho field đó; field có
   [AllowGenerics](../../Runtime/AllowGenerics/README.md) thì có thêm lớp generic và áp luật
   lọc của nó. Chọn *None* thì xóa giá trị.

Field hay kiểu không đổi được bằng cách chép field (một lớp generic mà field khác giữ giá trị theo
tham số kiểu của nó, chẳng hạn) gắn `[HideReplaceType]` để bỏ mục này khỏi menu: ở field thì áp
cho field đó, ở class hay interface thì áp cho mọi field khai kiểu đó.

Kết quả:

- Field được serialize của kiểu mới mà cùng tên với field được serialize của giá trị cũ,
  và nhận được giá trị đó, được chuyển sang **nguyên như cũ**. Giá trị
  `[SerializeReference]` con đi theo đúng instance, không bị tạo lại: đổi một Parallel
  thành Stagger vẫn giữ nguyên danh sách con.
- Field khác trong cùng object đang dùng chung giá trị cũ được trỏ sang giá trị mới.
  `[SerializeReference]` không dùng chung được giữa hai object, nên không có tham chiếu
  nào ở ngoài.
- Một lần `Ctrl+Z` hoàn tác cả thao tác.
- Chọn đúng kiểu đang có thì bỏ qua, có cảnh báo.
