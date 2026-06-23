# DirectoryWatcher
A .NET 8 web application and service for monitoring directory changes and file logging.

Aplikace je postavena na frameworku .NET 8 (ASP.NET Core MVC / REST API). Sledování změn v adresáři funguje na bázi snapshotů (snímků stavu) vyvolaných manuálně uživatelem (stiskem tlačítka v UI / zavoláním endpointu), což přesně odpovídá zadání.

Při analýze se porovnává aktuální stav prvků na disku s předchozím uloženým stavem. Změna obsahu souborů se detekuje pomocí porovnání MD5 hashů. Data o stavu souborů a jejich verzích jsou perzistována v paměti (In-Memory / JSON soubor) bez použití databáze.

Omezení řešení
Perzistence v paměti: Jelikož zadání zakazuje databázi, stav se drží v paměti aplikace. Při restartu webového serveru (aplikace) se historie verzí vynuluje.

Manuální trigger: Změny nejsou detekovány v reálném čase (real-time), ale až v momentě požadavku uživatele.

Škálovatelnost (Velikost a počet souborů): Architektura počítá s limity ze zadání (do 100 souborů v adresáři, velikost do 50 MB). Výpočet MD5 hashe probíhá synchronně/asynchronně v paměti serveru. Pro tisíce velkých souborů by bylo nutné streamování hashe a optimalizace paměti.
