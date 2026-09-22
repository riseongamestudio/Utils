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
