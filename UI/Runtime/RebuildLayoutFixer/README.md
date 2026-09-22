# RebuildLayoutFixer

[← RiseOn.Utils.UI](../../README.md)

Ép layout dựng lại ngay khi object được bật. Dùng cho các layout lồng nhau
(layout group trong layout group, `ContentSizeFitter`) hiện sai kích thước ở
frame đầu sau khi bật.

## Cách dùng

Thêm component vào RectTransform gốc của cụm layout bị sai.

- Ở `Update` đầu tiên sau khi được bật, nó gọi
  `LayoutRebuilder.ForceRebuildLayoutImmediate` cho RectTransform của mình, rồi
  tự tắt.
- Trong Inspector có nút *Fix* để dựng lại bằng tay trong Editor.
- Chạy lại lúc runtime: bật lại component (`fixer.enabled = true`, chạy ở frame
  kế tiếp) hoặc gọi `fixer.Fix()` (chạy ngay).
