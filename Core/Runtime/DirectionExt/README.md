# DirectionExt

[← RiseOn.Utils](../../README.md)

Một hướng serialize được: `Left`, `Right`, `Up`, `Down`, hoặc `Custom` với một góc tính bằng độ.

```csharp
[SerializeField] private DirectionExt shadow = new(Direction.Down);

var vector = shadow.ToVector2();   // độ dài 1
```

- Góc theo quy ước toán học và Unity: 0° là bên phải, tăng ngược chiều kim đồng hồ.
- `ToVector2()` trả về vector độ dài 1: 45° ra (0.71, 0.71).
- Trong Inspector (Odin), cả field nằm trên một dòng. `Custom` hiện thêm một mũi tên: nhấn
  hay kéo chuột trên nó để xoay theo chuột, góc làm tròn tới độ nguyên; cạnh đó là ô nhập số
  độ. Đổi từ hướng có sẵn sang `Custom` thì góc bắt đầu từ hướng cũ. Drawer nằm ở
  `Editor/DirectionExt`.
