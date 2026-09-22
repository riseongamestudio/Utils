# Bounds2D

[← RiseOn.Utils](../../README.md)

`Bounds` bản 2D: struct serialize được, đổi ngầm qua lại với `Bounds` (z = 0).

```csharp
[SerializeField] private Bounds2D cameraLimit;

var area = new Bounds2D(center: Vector2.zero, size: new Vector2(10, 6));
area.Encapsulate(player.position);          // nới để chứa điểm
area.Expand(1f);                            // mỗi chiều nới thêm 1
if (area.Contains(point)) { /* ... */ }
if (area.Intersects(other)) { /* ... */ }
var nearest  = area.ClosestPoint(point);
var bounds3D = (Bounds)area;
var fromCorners = Bounds2D.FromMinMax(min, max);
```

| Nhóm | Thành viên |
|---|---|
| Kích thước | `center`, `extents`, `size`, `min`, `max`, `xMin`, `yMin`, `xMax`, `yMax`, `SetMinMax` |
| Tạo | `new Bounds2D(center, size)`, `new Bounds2D(Bounds)`, `new Bounds2D(a, b)` (bao cả hai), `FromMinMax` |
| Nới | `Encapsulate(point)`, `Encapsulate(bounds)`, `Expand(float)`, `Expand(Vector2)` |
| Kiểm tra | `Contains(point)`, `Contains(bounds)`, `Intersects`, `Intersects_X`, `Intersects_Y` |
| Khoảng cách | `ClosestPoint`, `Distance`, `SqrDistance` |
| Nội suy | `Lerp`, `LerpUnclamped` |
