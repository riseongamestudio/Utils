# NetworkTime

[← RiseOn.Utils.Network](../../README.md)

Lấy giờ hiện tại từ mạng, không phụ thuộc đồng hồ của máy. Dùng cho những chỗ
người chơi có thể chỉnh giờ máy để gian lận: quà theo ngày, thời gian hồi, sự
kiện có hạn.

## Cách dùng

```csharp
using RiseOn.Utils.Network;

DateTime utcNow = await NetworkTime.NowAsync();
DateTime utcNow = await NetworkTime.NowAsync(timeoutSeconds: 5f, cancellationToken: token);
```

- Trả về `DateTime` có `Kind` là `Utc`. Cần giờ địa phương thì tự đổi, ví dụ
  `utcNow.ToLocalTime()`.
- Hết `timeoutSeconds` (mặc định 2 giây) mà không nguồn nào trả lời thì ném
  `TimeoutException`. `Message` liệt kê lỗi của từng lần thử, `InnerException` là
  `AggregateException` chứa các lỗi gốc.
- Hủy `cancellationToken` thì ném `OperationCanceledException`.
- `timeoutSeconds` không dương hoặc không hữu hạn thì ném
  `ArgumentOutOfRangeException` ngay lúc gọi.

## Cách lấy giờ

Mỗi vòng thử lần lượt:

1. **NTP** qua UDP cổng 123: `time.google.com`, `time.cloudflare.com`,
   `pool.ntp.org`, `time.windows.com`. Mỗi server tối đa 350 ms, chính xác tới
   mili giây.
2. **Header `Date`** của response HTTPS: `www.google.com/generate_204`,
   `www.cloudflare.com/cdn-cgi/trace`. Mỗi lần tối đa 600 ms, chính xác tới giây.
   Dùng khi mạng chặn UDP, như một số wifi công ty hay trường học.

Hết một vòng mà chưa có kết quả thì nghỉ 50 ms rồi thử lại từ đầu, tới khi hết
thời gian. Địa chỉ IP của từng server NTP được nhớ lại sau lần phân giải DNS đầu.

Giờ trả về chưa trừ thời gian gói tin đi về, thường vài chục mili giây. Đủ cho
logic game, không dùng để đồng bộ chính xác.

## Nền tảng

Chạy trên Editor, Android, iOS, desktop và WebGL. Trình duyệt không có UDP nên
WebGL bỏ NTP, đọc giờ từ `ts=` trong `cdn-cgi/trace` của Cloudflare
(`www.cloudflare.com`, `1.1.1.1`), dự phòng bằng `dateTime` của `timeapi.io`, rồi
cộng thêm nửa thời gian đi về. Trên WebGL phải gọi từ main thread.
