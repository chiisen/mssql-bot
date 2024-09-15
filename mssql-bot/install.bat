echo 【請輸入 .\install.bat】

cd mssql-bot

echo 【解除安裝全域套件】
del .\nupkg\*.nupkg

echo 【打包全域套件】
dotnet pack

echo 【解除安裝全域套件】
dotnet tool uninstall -g mssql-bot

echo 【安裝全域套件】
dotnet tool install --global --add-source .\nupkg mssql-bot

echo 【查看全域套件】
dotnet tool list -g

cd ..