# 고철 킥! 리소스 목록

현재 게임에서 이미지 18개와 효과음 8개를 사용한다. GameAssets.cs가 실행 파일 옆 resource에서 로딩한다.

| 폴더 | 파일 (이미지 PNG / 소리 WAV) |
| --- | --- |
| image/player | player_idle, player_kick, player_walk_sheet |
| image/enemy | enemy_basic, enemy_flying, enemy_walk_sheet |
| image/stage | arena_floor |
| image/effect | kick_arc, hit_spark_sheet, enemy_break_sheet, wall_hit_sheet |
| image/ui | hp_full, hp_empty, kick_ready, kick_cooldown, scrap_hud, scrap_result, title_logo |
| sound/sfx | kick_swing, metal_hit, wall_hit, player_hurt, enemy_break, clear, game_over, restart |

걷기와 충돌·파괴 시트는 256×256이며 128×128 프레임 4개를 2×2로 배치했다. 프레임 순서는 좌상→우상→좌하→우하이다.

사용하지 않는 이전 result_panel과 status_icons 합본은 제거했다. 모든 현재 PNG/WAV는 코드에서 사용한다. 배경음악은 없다.

제목은 참고 로고 느낌으로 새로 그린 title_logo.png를 사용한다. 결과 제목은 HY헤드라인M, 안내와 숫자는 HY견고딕이며, 폰트 파일은 포함하지 않는다.

제작 정보와 규격은 ASSET_NOTES.md, 과거 추가 그래픽 제작 과정은 docs/추가그래픽_제작기록.md를 참고한다. 과거 기록에 나오는 detail/ui_redraw 경로는 현재 player/enemy/effect/ui로 정리되어 있다.