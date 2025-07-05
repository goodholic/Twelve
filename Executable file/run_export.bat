@echo off
echo Unity 데이터를 CSV로 내보내는 중...
python export_unity_data_to_excel.py
echo.
echo CSV 파일 생성이 완료되었습니다!
echo 생성된 파일들:
echo - ally_characters.csv (아군 캐릭터 전체)
echo - enemy_characters.csv (적 캐릭터 전체)
echo - ally_one_star_characters.csv (아군 1성)
echo - enemy_one_star_characters.csv (적 1성)
echo - two_star_characters.csv (아군 2성 - 기존)
echo - three_star_characters.csv (아군 3성 - 기존)
echo - ally_two_star_characters.csv (아군 2성 - 새로운)
echo - ally_three_star_characters.csv (아군 3성 - 새로운)
echo - enemy_two_star_characters.csv (적 2성)
echo - enemy_three_star_characters.csv (적 3성)
echo - items_new.csv (아이템)
pause 