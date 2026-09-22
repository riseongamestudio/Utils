# Extension cho renderer

[← RiseOn.Utils.Renderers](../../README.md)

Namespace `RiseOn.Utils.Renderers`.

## SpriteRendererExtensions

```csharp
sprite.SetAlpha(0.5f);   // chỉ đổi alpha của SpriteRenderer.color
```

## ParticleSystemExtensions

Phát hạt tại một điểm cho trước, không phải dời ParticleSystem:

```csharp
vfx.EmitAt(hitPoint, amount: 10);                          // phát 10 hạt ngay
vfx.BurstAt(this, hitPoint, amount: 5, interval: 0.05f);   // 5 hạt, cách nhau 0.05 giây
vfx.BurstAllAt(this, hitPoint);                            // phát lại mọi burst khai trong module Emission
```

- `BurstAt` và `BurstAllAt` cần một `MonoBehaviour` (tham số thứ hai) để chạy
  coroutine.
- `BurstAllAt` giữ nguyên thời điểm, số chu kỳ, khoảng lặp và xác suất của từng
  burst.
- `applyShape: true` áp thêm module Shape (vị trí ngẫu nhiên theo hình dạng
  phát) quanh điểm đó.
