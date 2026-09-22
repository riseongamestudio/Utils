# EnumLabel

[← RiseOn.Utils](../../README.md)

Attribute cho Odin: đặt nhãn phần tử mảng theo tên các giá trị của một enum.

```csharp
public enum Sfx { Click, Win, Lose }

[EnumLabel(typeof(Sfx))]
[SerializeField] private AudioClip[] clips;   // phần tử 0, 1, 2 hiện nhãn Click, Win, Lose
```

- Chỉ áp cho mảng một chiều.
- Phía trên mảng có ô *Enum Type* để đổi sang enum khác ngay trong Inspector.
  Lựa chọn đó được nhớ riêng cho từng object ở phía Editor, không ghi vào asset;
  kiểu truyền vào attribute (có thể bỏ trống) là giá trị mặc định.
- Inspector cảnh báo khi độ dài mảng khác số giá trị của enum.
- Attribute chỉ tồn tại trong Editor, không tốn gì lúc chạy. Cần Odin Inspector
  để vẽ; drawer nằm ở `Editor/EnumLabel`.
