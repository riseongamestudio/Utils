# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils.ui` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.1] - 2026-09-25

### Sửa

- Các assembly dùng Odin có thêm `defineConstraints: ODIN_INSPECTOR`: thiếu Odin thì chúng
  được bỏ qua, và lỗi giải thích nằm ở `RiseOn.Utils.Requirements`.
- Phụ thuộc tối thiểu `com.riseon.utils` 1.0.1.

## [1.0.0] - 2026-09-23

### Thêm

- `AspectRatioFitterElement`: layout element giữ tỉ lệ khung trong layout group.
- `RebuildLayoutFixer`: ép layout dựng lại khi được bật.
- `ImageUtils` (`SetColorR/G/B/A`, `SetColor(index, value)`).
- Phụ thuộc UniTask (`com.cysharp.unitask` 2.5.11), dùng để chờ frame trong
  `AspectRatioFitterElement`.
