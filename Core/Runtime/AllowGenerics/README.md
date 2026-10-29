# AllowGenerics

[← RiseOn.Utils](../../README.md)

Ô chọn kiểu của Odin cho field `[SerializeReference]` không liệt kê lớp generic khi
không suy ra được tham số từ kiểu của field. Ví dụ field kiểu `IShape` không bao giờ
thấy `Tagged<T>`. Gắn `[AllowGenerics]` thì ô chọn có thêm các lớp đó, và chọn vào
thì hỏi từng tham số mà field không quyết định.

```csharp
public interface IShape { }

[Serializable] public class Circle : IShape { }
[Serializable] public class Tagged<T> : IShape { public T tag; }

public class Board : MonoBehaviour {
    [SerializeReference, AllowGenerics] private IShape shape;
}
```

Gắn ở **class hay interface** thì mọi field khai báo kiểu đó đều được, chỗ dùng không
phải gắn gì:

```csharp
[AllowGenerics]
public interface IShape { }
```

Áp cho field `[SerializeReference]` thường, giá trị trong `SerRef<T>` và từng phần tử
của `ListSerRef<T>` hay của list có `[SerializeReference]`.

## Trong Inspector

Field vẫn do Odin vẽ, trông và dùng y như mọi field `[SerializeReference]`. Chỉ khi
bấm nút mở ô chọn, ô chọn của Odin được thay bằng một
[cửa sổ tìm kiếm](../../Editor/SearchWindow/README.md) dạng drop-down, cùng chỗ, cùng
chiều ngang:

1. Trang đầu: những gì Odin vẫn đưa ra, cộng các lớp generic như `Tagged<T>`. Lớp
   generic được suy tham số từ kiểu của field (xem [bên dưới](#suy-tham-số-từ-field)):
   suy đủ thì hiện sẵn kiểu đã đóng, suy được một phần thì hiện tên kèm phần đã biết,
   như `Tagged<int, TValue>`.
2. Chọn một lớp generic còn thiếu tham số thì cửa sổ chuyển sang trang chọn tham số
   còn thiếu đầu tiên, dòng tiêu đề ghi kiểu đang dựng: `Tagged<▢>   ·   T`.
3. Tham số chọn vào lại là generic còn mở (`List<T>`) thì hỏi tiếp tham số của nó:
   `Tagged<List<▢>>   ·   T`. Lồng sâu bao nhiêu cũng được.
4. Đủ tham số thì tạo instance của kiểu đã đóng và gán vào field.

Mục *Change Type* trong menu chuột phải là của Odin: nó mở ô chọn ngoài lượt vẽ của field, nên
vẫn ra ô chọn gốc của Odin (không có lớp generic) và tạo instance mới. Đổi kiểu mà cần generic
thì dùng nút mở ô chọn, hoặc [Replace Type](../../Editor/Tools/README.md#replace-type) nếu muốn giữ
dữ liệu: trên field có `AllowGenerics`, ô chọn của Replace Type cũng có lớp generic.

Mọi trang nằm trong cùng một cửa sổ, chuyển trang không đóng mở lại. Kiểu được xếp vào
thư mục theo `[TypeRegistryItem]` như ô chọn của Odin (đường dẫn và tên khai trong đó),
kiểu không có attribute này thì theo namespace; hai kiểu trùng tên trong một thư mục thì
kèm namespace. Trang tham số chỉ liệt kê kiểu thỏa ràng buộc C# của tham số (`class`,
`struct`, `new()`, kiểu gốc). Ràng buộc nối các tham số với nhau được kiểm khi đã đủ tham
số; không thỏa thì báo lỗi, không tạo.

Kiểu bị ẩn trong cài đặt *Type Selector* của Odin vẫn hiện, vì Odin không mở cách đọc cài
đặt đó ra ngoài.

## Suy tham số từ field

Field kiểu generic đã đóng, như `IConverter<string, int>`, quyết định một phần hay toàn bộ
tham số của lớp chọn vào. Trên field có `AllowGenerics`, ô chọn tự suy các tham số đó,
thay cho cách Odin tự làm:

- **Theo đúng vị trí trong khai báo**, không theo thứ tự tham số. `Parse<T, TFormat> :
  IConverter<string, T>` cho field `IConverter<string, int>` ra `T = int`, còn `TFormat`
  để hỏi. Odin ghép theo thứ tự nên ở đây sẽ ghép nhầm `TFormat = int`.
- **Qua variance**: lớp cài `IConverter<object, T>` vào được field
  `IConverter<string, int>` khi tham số đầu của interface là `in`. Odin bỏ sót những lớp
  này.
- Lớp mâu thuẫn với field (cài `IConverter<int, T>` cho field `IConverter<string, int>`)
  không hiện.

Field không có `AllowGenerics` thì danh sách giữ nguyên như của Odin.

## Làm xám kiểu không dùng được

Viết `GetDisabledReason` để chỉ ra kiểu nào, dù hợp với field, vẫn không nên chọn. Trả
lý do thì kiểu đó hiện màu xám kèm lý do trong ngoặc, và bấm vào không có tác dụng; trả
`null` là chọn được.

```csharp
internal sealed class AllowFormattedAttribute : AllowGenericsAttribute {
    public override string GetDisabledReason(Type type) {
        return Formatter.Has(type) ? null : "no formatter";
    }
}
```

Hàm nhận kiểu đã đóng: kiểu ở trang đầu, hoặc kiểu vừa dựng xong sau khi chọn đủ tham
số. Kiểu dựng xong mà bị chặn thì báo lỗi kèm lý do, không gán.

## Lọc tham số

Kế thừa `AllowGenericsAttribute` và viết `Accepts` để thu hẹp những gì được điền vào
tham số của lớp generic chọn thẳng vào field. Tham số của generic lồng bên trong chỉ
lọc theo ràng buộc C# của chính nó.

```csharp
// Only object types, since the shape is dragged in as an object reference.
internal sealed class AllowObjectTagsAttribute : AllowGenericsAttribute {
    public override bool Accepts(Type parameter, Type argument) {
        return typeof(UnityEngine.Object).IsAssignableFrom(argument);
    }
}

[AllowObjectTags]
public interface IShape { }
```

- `parameter` là chính tham số kiểu (`T`), nên biết được nó thuộc lớp nào và ở vị trí
  nào.
- `argument` có thể là một định nghĩa generic còn mở; trả `false` thì không cho lồng.
- Hàm chạy trong Editor nhưng lớp nằm ở runtime: dùng reflection thường, không dùng API
  của `UnityEditor`.

Cần Odin Inspector; drawer và ô chọn nằm ở `Editor/AllowGenerics`. Đổi kiểu một giá
trị đã có mà giữ dữ liệu thì dùng [Replace Type](../../Editor/Tools/README.md#replace-type).
