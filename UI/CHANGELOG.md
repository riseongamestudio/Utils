# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils.ui` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.5] - 2026-10-21

### Đổi

- Inspector của `RaycastBlocker` bỏ luôn ô Script, chỉ còn *Raycast Target* và *Raycast Padding*.

## [1.0.4] - 2026-10-21

### Sửa

- `RaycastBlocker` bắt buộc có `CanvasRenderer`. UGUI 2.0 chỉ thêm component này lúc Graphic cần tới,
  nên blocker đang tắt nằm trong prefab không có nó, và Prefab Mode không coi prefab đó là UI.

### Thêm

- Inspector của `RaycastBlocker` chỉ hiện *Raycast Target* và *Raycast Padding*, bỏ Color và Material
  vì blocker không vẽ gì. Nằm trong assembly mới `RiseOn.Utils.UI.Editor`.

## [1.0.3] - 2026-10-14

### Thêm

- `RaycastBlocker`: vùng trong suốt nhận raycast nhưng không vẽ gì, để chặn cú bấm xuống các phần tử phía sau.

### Đổi

- Tên hiển thị trong Package Manager thành `RiseOn Utils UI`.

## [1.0.2] - 2026-09-25

### Sửa

- Pack tự dùng Odin nên có assembly `RiseOn.Utils.UI.Requirements` của riêng nó: thiếu Odin Inspector,
  hoặc thiếu define `ODIN_INSPECTOR` ở nền tảng đang chọn, thì báo một lỗi nói rõ pack này cần Odin.

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
