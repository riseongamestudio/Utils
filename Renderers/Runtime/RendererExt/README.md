# RendererExt

[← RiseOn.Utils.Renderers](../../README.md)

Đổi pivot của sprite ngay trên object, không phải sửa pivot trong Sprite Editor,
và chỉnh renderer qua một component ở object cha.

Hai bản cụ thể: `SpriteRendererExt` và `SpriteMaskExt`.

## Cách dùng

1. Thêm `SpriteRendererExt` (hoặc `SpriteMaskExt`) vào một GameObject. Component
   tạo sẵn GameObject con tên `Renderer` chứa renderer; nếu đã có con tên
   `Renderer` mang đúng loại renderer thì dùng luôn.
2. Chỉnh renderer ngay trong Inspector của component: phần trên cùng vẽ lại
   Inspector của renderer con (sprite, màu, sorting...).
3. Chọn *Pivot*.

| *Pivot* | Ý nghĩa |
|---|---|
| `Unchanged` | Giữ pivot gốc của sprite |
| `Center`, `TopLeft`, `Top`, `TopRight`, `Left`, `Right`, `BottomLeft`, `Bottom`, `BottomRight` | Điểm đó của sprite nằm đúng gốc tọa độ của object cha |
| `Custom` | Nhập *Custom Pivot* theo tỉ lệ 0–1 của sprite |

Pivot tính cả khi sprite bị lật. Trong Editor, đổi sprite, pivot gốc hay
pixels-per-unit của sprite thì vị trí tự cập nhật. Lúc chạy, đổi qua property của
component (`Sprite`, `FlipX`...) để vị trí được căn lại theo.

Từ code:

```csharp
var ext = GetComponent<SpriteRendererExt>();
ext.Sprite = newSprite;               // cập nhật vị trí theo pivot ngay
ext.Pivot  = PivotOverrideMode.Bottom;
ext.FlipX  = true;
ext.Size   = new Vector2(2, 1);       // SpriteRendererExt: kích thước khi Draw Mode là Sliced / Tiled
ext.Rdr.color = Color.red;            // renderer con
```

| Thành viên | Ý nghĩa |
|---|---|
| `Rdr` | Renderer con |
| `Sprite`, `FlipX`, `FlipY` | Đổi trên renderer con rồi căn lại |
| `DrawMode`, `Size` | Chỉ có ở `SpriteRendererExt` |
| `Pivot`, `CustomPivot` | Pivot ghi đè |
| `UpdateRdrPosFromPivot()` | Căn lại ngay (cũng là nút trong Inspector) |

## Lưu ý

- Transform của object con bị khóa trong Inspector: rotation luôn 0, scale đổi
  ở con được dồn lên cha, vị trí do component đặt. Muốn xoay hay phóng to thì
  làm trên object cha.
- `SpriteMask` không có flip nên `SpriteMaskExt` tự lưu `FlipX` / `FlipY` và lật
  bằng scale âm của object con.
- Inspector báo lỗi khi thiếu renderer con (kèm nút tạo) và cảnh báo khi renderer
  không phải con trực tiếp (kèm nút sửa).
