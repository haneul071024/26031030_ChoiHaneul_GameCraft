# 고철 킥! 리소스 목록

현재 게임에서 이미지 19개, 효과음 8개, 배경음악 1개를 사용한다. GameAssets.cs가 실행 파일 옆 resource에서 로딩한다.

| 폴더 | 파일 (이미지 PNG / 소리 WAV) |
| --- | --- |
| image/player | player_idle, player_kick, player_walk_sheet |
| image/enemy | enemy_basic, enemy_flying, enemy_walk_sheet |
| image/stage | arena_floor, electric_tile_sheet |
| image/effect | kick_arc, hit_spark_sheet, enemy_break_sheet, wall_hit_sheet |
| image/ui | hp_full, hp_empty, kick_ready, kick_cooldown, scrap_hud, scrap_result, title_logo |
| sound/sfx | kick_swing, metal_hit, wall_hit, player_hurt, enemy_break, clear, game_over, restart |
| sound/bgm | scrapyard_loop (로비·전투 반복 재생, M 토글) |

걷기와 충돌·파괴 시트는 256×256이며 128×128 프레임 4개를 2×2로 배치했다. 프레임 순서는 좌상→우상→좌하→우하이다.

사용하지 않는 이전 result_panel과 status_icons 합본은 제거했다. 모든 현재 PNG/WAV는 코드에서 사용한다. 배경음악은 sound/bgm/scrapyard_loop.wav (16초, 44.1kHz, 모노 PCM16)이다.

제목은 참고 로고 느낌으로 새로 그린 title_logo.png를 사용한다. 결과 제목은 맑은 고딕, 안내와 숫자는 맑은 고딕이며, 폰트 파일은 포함하지 않는다.

이미지는 OpenAI image_gen으로 생성하고 크기 조절·프레임 분할을 적용했다. 효과음과 BGM은 외부 음원·샘플 없이 파형과 노이즈를 합성했다. BGM 재생성 스크립트는 tools/Make-Bgm.ps1이다. 전기 바닥 시트는 1254×1254이며, 627×627 프레임 4개를 2×2로 배치했다.
