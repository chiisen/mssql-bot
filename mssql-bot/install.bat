echo �i�п�J .\install.bat�j

cd mssql-bot

echo �i�Ѱ��w�˥���M��j
del .\nupkg\*.nupkg

echo �i���]����M��j
dotnet pack

echo �i�Ѱ��w�˥���M��j
dotnet tool uninstall -g mssql-bot

echo �i�w�˥���M��j
dotnet tool install --global --add-source .\nupkg mssql-bot

echo �i�d�ݥ���M��j
dotnet tool list -g

cd ..