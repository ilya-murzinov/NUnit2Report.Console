@echo off
for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set ldt=%ldt:~0,4%-%ldt:~4,2%-%ldt:~6,2%_%ldt:~8,2%-%ldt:~10,2%-%ldt:~12,6%

echo %ldt%
mkdir %cd%\Results\[%ldt%]

"C:\Program Files\NUnit 2.6.3\bin\nunit-console.exe" "Tests.dll" /out:%cd%\Results\[%ldt%]\Tests.txt
"C:\Users\Ilya\Documents\Visual Studio 2013\Projects\NUnit2Report.Console\src\NUnit2Report.Console\bin\Debug\NUnit2Report.Console.exe" -o Tests.html --todir %cd%\Results\[%ldt%] --fileset=TestResult.xml --lang en

pause