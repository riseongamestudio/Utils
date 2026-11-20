# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils.litmotion` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.1] - 2026-11-20

### Đổi

- Cần Unity 6000.3, bản đang dùng để phát triển: `package.json` ghi `unity`, nên Package
  Manager không cài pack vào project Unity cũ hơn.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 1.0.18.

## [1.0.0] - 2026-10-29

### Thêm

- `EaseExt`: một `Ease` của LitMotion, hoặc một `AnimationCurve` tự vẽ khi chọn
  `CustomAnimationCurve`. Inspector của Odin vẽ trên một dòng; ô curve chỉ hiện cùng hàng,
  chia đôi cột giá trị, khi chọn `CustomAnimationCurve`.
