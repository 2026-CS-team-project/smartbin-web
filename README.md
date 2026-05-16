# SmartBin

스마트 쓰레기통 관리 시스템 - ASP.NET Blazor 기반 웹 애플리케이션입니다.  
쓰레기통(Bin) 상태 모니터링, 수거 트럭(Truck) 관리, 알림(Alert) 기능을 제공합니다.

## 기술 스택

- **프레임워크**: ASP.NET Core 8.0 (Blazor Web App)
- **언어**: C#
- **UI**: Blazor Components

## 사전 요구사항

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 이상

## 실행 방법

```bash
# 저장소 클론
git clone https://github.com/<YOUR_USERNAME>/SmartBin.git
cd SmartBin

# 패키지 복원 및 실행
dotnet run
```

브라우저에서 `https://localhost:5001` 또는 `http://localhost:5000` 으로 접속합니다.

## 프로젝트 구조

```
SmartBin/
├── Components/       # Blazor 컴포넌트 및 페이지
│   └── Pages/        # 라우팅 페이지 (Home, Bins, BinDetail, Alerts)
├── Models/           # 데이터 모델 (Bin, Truck, Alert)
├── Services/         # 비즈니스 로직 서비스
├── wwwroot/          # 정적 파일 (CSS, JS)
├── Program.cs        # 애플리케이션 진입점
└── appsettings.json  # 설정 파일
```

## 라이선스

MIT
