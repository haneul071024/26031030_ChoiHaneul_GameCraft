# glc2d 출처

- 작성자: 3dapi (오희성 교수님)
- 예제: https://github.com/3dapi/90018899_HeesungOh_GameCraft/tree/f1/src/glc2d
- 기준 릴리스: f1 (기말 1차)
- 프로젝트 내 위치: src/ActionGame/glc2d
- 가져온 항목: 해당 폴더의 C# 소스 11개 및 README.md
- 원본의 작성자 주석을 유지했다. 실행하면서 문제가 있었던 세 파일을 수정했다.
  - G2D2DContext.cs: 96 DPI 설정으로 Windows 배율과 화면 변환의 중복 확대 방지
  - G2AppBase.cs: 모니터 DPI와 작업 영역에 맞게 초기 창 크기 조절
  - G2InputContext.cs: 짧은 키 누름을 프레임 사이에서 놓치지 않도록 이벤트 기록 추가
- 교수님 예제처럼 소스를 프로젝트 안에 넣어 사용한다.
- 게임 코드는 src/ActionGame/GameMain.cs, Player.cs, Enemy.cs에 따로 작성했다.
- 이 폴더에 별도의 라이선스 안내는 없었다. 수업 외 용도로 배포할 때는 사용 조건을 확인해야 한다.
