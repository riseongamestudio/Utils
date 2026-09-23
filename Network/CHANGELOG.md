# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.utils.network` được ghi ở đây. Định dạng
theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.0] - 2026-09-23

### Thêm

- `NetworkTime.NowAsync`: giờ UTC từ NTP, dự phòng bằng header `Date` của HTTPS.
  Thay cho `SerMoment.NowNetwork` của `com.riseon.serializables`, thêm phần mili
  giây, qua được mốc tràn năm 2036 của NTP, hủy được bằng `CancellationToken`, và
  request HTTP quá hạn bị hủy luôn thay vì chạy ngầm tiếp.
- `Connectivity.HasNetwork`: máy có kết nối mạng nào không, không gửi request.
- `Connectivity.HasInternetAsync`: có vào được internet thật không, nhận ra cả
  wifi bắt đăng nhập.
