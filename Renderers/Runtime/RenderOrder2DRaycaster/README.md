# RenderOrder2DRaycaster

[← RiseOn.Utils.Renderers](../../README.md)

Raycaster cho EventSystem, dùng thay `Physics2DRaycaster`. Khi nhiều Collider2D
chồng lên nhau dưới con trỏ, object được **vẽ trên cùng** nhận sự kiện trước,
thay vì thứ tự mà `Physics2DRaycaster` trả về.

## Cách dùng

1. Scene có `EventSystem`; object cần nhận click có `Collider2D` và script cài
   `IPointerClickHandler`, `IPointerDownHandler`...
2. Thêm `RenderOrder2DRaycaster` vào Camera.
3. Gỡ `Physics2DRaycaster` trên Camera đó nếu có, không thì mỗi hit bị trả hai
   lần.

| Trường | Ý nghĩa |
|---|---|
| *Event Mask* | Layer được raycast, mặc định tất cả |

Trigger có được tính hay không theo `Physics2D.queriesHitTriggers`, giống
`Physics2D.OverlapPoint`.

## Thứ tự so sánh

Hai object được so theo đúng cách Unity quyết định cái nào vẽ sau:

1. Chuỗi `SortingGroup` từ gốc xuống (tôn trọng *Sort At Root*), so ở cấp đầu
   tiên khác nhau.
2. Sorting layer, rồi *Order in Layer*, rồi vị trí z.
3. Hòa hết: object chứa (cha) nhận trước object nằm trong nó; object có
   `SpriteRenderer` nhận trước object không có.
4. Vẫn hòa: Unity không định nghĩa thứ tự vẽ cho trường hợp này, nên hàm trả 0.
   Đặt *Order in Layer* khác nhau để phân định.

Hàm so sánh dùng lại được cho việc khác:

```csharp
// âm nếu a vẽ sau b (a nằm trên)
int order = RenderOrder2DRaycaster.Compare(a.transform, b.transform);
```
