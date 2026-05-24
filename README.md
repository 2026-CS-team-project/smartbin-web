# SmartBin

스마트 쓰레기통 관리 시스템 - ASP.NET Blazor 기반 웹 애플리케이션
쓰레기통(Bin) 상태 모니터링, 수거 트럭(Truck) 관리, 알림(Alert), Unity WebGL 시뮬레이션 임베드 기능을 제공합니다.

## 기술 스택

- **프레임워크**: ASP.NET Core 8.0 (Blazor Web App, Interactive Server)
- **언어**: C#
- **UI**: Blazor Components
- **데이터베이스**: Supabase (PostgREST 클라이언트 `supabase-csharp`)
- **시뮬레이션**: Unity WebGL 빌드 임베드 (`wwwroot/webgl/`)

## 주요 기능

- **대시보드**: 전체 쓰레기통·수거차 통계 요약, Unity WebGL 시뮬레이션 오버뷰 표시
- **쓰레기통 목록 / 상세**: 적재율·좌표·상태 표시, 상세 페이지에서 해당 쓰레기통으로 WebGL 카메라 자동 포커싱
- **수거 트럭 목록 / 상세**: 적재량·수거 횟수·목적지 표시, 상세 페이지에서 해당 트럭으로 WebGL 카메라 자동 포커싱
- **알림 센터**: 쓰레기통 적재율이 70% 또는 90%를 초과할 때 실시간 알림 생성, 읽지 않은 알림 수 사이드바 뱃지 표시
- **새로고침**: 목록·상세 페이지 모두 페이지 재로드 없이 DB 최신 데이터만 받아오는 새로고침 버튼 제공

## 사전 요구사항

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 이상
- Supabase 프로젝트 (URL, Anon Key)

## 설정

`appsettings.json` 또는 사용자 시크릿에 Supabase 접속 정보가 필요.

```json
{
  "Supabase": {
    "Url": "https://<your-project>.supabase.co",
    "AnonKey": "<your-anon-key>"
  }
}
```

> **주의**: 커밋된 키는 anon(public) 키여야 하며 `service_role` 키는 절대 클라이언트/저장소에 저장하지 마세요.
> 운영 환경에서는 환경변수 `Supabase__Url`, `Supabase__AnonKey` 또는 사용자 시크릿 사용을 권장합니다.

### Supabase 측 체크리스트

- `trash_bin_state_latest`, `trash_truck_state_latest` 등 모델이 참조하는 **테이블/뷰가 존재**해야 합니다.
- 각 테이블의 **RLS(Row Level Security) 정책**이 anon 키에 대해 `SELECT` 를 허용해야 합니다.

## 실행 방법

```bash
# 저장소 클론
git clone https://github.com/2026-CS-team-project/smartbin-web.git
cd smartbin-web

# 패키지 복원 및 실행
dotnet run
```

실행 후 터미널에 표시되는 URL(예: `https://localhost:xxxx`)로 브라우저에서 접속합니다.

## 프로젝트 구조
smartbin-web/
├── Components/
│   ├── Pages/                # 라우팅 페이지
│   │   ├── Home.razor        # 대시보드
│   │   ├── Bins.razor        # 쓰레기통 목록
│   │   ├── BinDetail.razor   # 쓰레기통 상세 + WebGL 임베드
│   │   ├── Trucks.razor      # 수거 트럭 목록
│   │   ├── TruckDetail.razor # 수거 트럭 상세
│   │   └── Alerts.razor      # 알림
│   └── ...
├── Models/                   # Postgrest BaseModel 상속 데이터 모델
│   ├── Bin.cs
│   ├── Truck.cs
│   └── Alert.cs
├── Services/                 # Supabase 쿼리 래퍼 + 백그라운드 서비스
│   ├── BinService.cs
│   ├── TruckService.cs
│   ├── AlertService.cs           # 알림 싱글톤 (임계값 감지, 읽음 처리)
│   └── AlertBackgroundService.cs # 5초 주기 폴링 → AlertService.ProcessBins() 호출
├── wwwroot/
│   ├── app.css
│   └── webgl/                # Unity WebGL 빌드 (index.html, Build/, TemplateData/)
├── Program.cs                # 진입점 + Supabase/서비스 DI 등록
├── appsettings.json          # 설정 파일
└── SmartBin.csproj
```

## Unity WebGL 시뮬레이션 연동

### 구조

`wwwroot/webgl/index.html` 은 정적 파일로 서빙되며, `App.razor` 의 `<body>` 에 **단 하나의 `<iframe>`** 으로 선언됩니다.
페이지 이동(Blazor Enhanced Navigation) 중에도 iframe이 재생성되지 않으므로 Unity 인스턴스와 시뮬레이션 상태가 유지됩니다.

```
App.razor (정적 HTML 셸)
└── #sim-frame-host  ← position:fixed iframe (항상 존재)
     └── <iframe src="/webgl/index.html">
```

### 위치 오버레이

`wwwroot/js/persistentSim.js` 가 `requestAnimationFrame` 루프로 매 프레임마다:

1. 현재 페이지에 `#sim-anchor` 요소가 있으면 → iframe을 해당 요소 위에 정확히 오버레이 (z-index: 1)
2. `#sim-anchor` 가 없으면 → 마지막 위치를 유지하되 z-index: -1 로 숨김 (Unity rAF 스로틀링 방지)

### 카메라 포커싱

`#sim-anchor` 에 `data-bin` 속성으로 포커스 대상을 지정합니다.

| `data-bin` 값 | 호출되는 Unity 메서드 |
|---|---|
| `0` | `SendMessage("Main Camera", "ShowOverview")` |
| 양수 (1, 2, …) | `SendMessage("TrashCan Camera Navigator", "MoveToTrashCan", <int>)` |
| 음수 (-1, -2, …) | `SendMessage("Main Camera", "ShowTruck", <int>)` — 트럭 인덱스 = `abs(값) - 1` |

Blazor 컴포넌트에서 명시적으로 포커스를 보낼 때는 `smartbinSim.focusBin(<int>)` 를 호출합니다 (대시보드 진입 시 0 전송).

### WebGL 빌드 교체 시 주의

`wwwroot/webgl/index.html` 은 Blazor 연동 코드가 포함된 커스텀 버전입니다.
Unity 재빌드 후 파일을 교체할 때는 **`Build/` 폴더만 교체**하고 `index.html` 은 덮어쓰지 마세요.

`.gz` 정적 파일 서빙을 위해 `Program.cs` 에서 `FileExtensionContentTypeProvider` 와 `Content-Encoding: gzip` 헤더를 설정합니다.

## 새 테이블/엔티티 추가 시 절차

1. `Models/Xxx.cs` 에 `BaseModel` 상속 + [Table], [Column], [PrimaryKey] 속성 작성
2. `Services/xxxService.cs` 에 쿼리 메서드 작성
3. `Program.cs` 에 `builder.Services.AddScoped<xxxService>();` 등록
4. Razor 페이지에서 `@inject SmartBin.Services.xxxService xxxService` 로 사용
5. Supabase 콘솔에서 해당 테이블의 RLS 정책 확인