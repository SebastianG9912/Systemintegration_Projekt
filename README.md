# Projektuppgift Systemintegration

### Gruppmedlemmar

Shkelqim Cakaj,<br>
Sebastian Gustafsson,<br>
Ronni Söderberg<br>

Detta projekt simulerar ett bibliotek och lånetjänst. En användare kan skapa konto och låna böcker. En administratör (Admin) kan lägga till nya böcker och blockera användare från att logga in.

Projektet körs lokalt på docker där varje tjänst har sin egna container och databas. Tjänsterna "loanservice" och "libraryservice" kommunucerar via gRPC och "userservice" generarar en JWT-token då användaren loggat in.

## Kommandon (i terminalen):

### För migrations

```ps
//Navigera till mappen "UserService" och migrera
cd UserService
dotnet ef migrations add Initial

//Navigera till mappen "LibraryService" och migrera
cd LibraryService
dotnet ef migrations add Initial

//Navigera till mappen "LoanService" och migrera
cd LoanService
dotnet ef migrations add Initial
```

### För docker

```ps
//Navigera till mappen "UserService" och lägg upp imagen
cd UserService
docker build --rm -t userservice .

//Navigera till mappen "LibraryService" och lägg upp imagen
cd LibraryService
docker build --rm -t libraryservice .

//Navigera till mappen "LoanService" och lägg upp imagen
cd LoanService
docker build --rm -t loanservice .

//Gå tillbaks till huvudmappen och kör .yml filen
cd ..
docker-compose -f docker-compose.yml up
```

## En tjänst för anvädarkonto:

Registrera<br>
Logga in<br>
(Endast Admin nedanför)<br>
Se alla användare<br>
Ändra användarinformation<br>
Blockera inloggning för användare<br>

Användartjänsten har en användarmodell som innehåller id, namn, email. Håller bara koll på användarna.

## En tjänst (bibliotek) med register över alla böcker:

Se alla böcker<br>
Se specifik bok<br>
(Endast Admin nedanför)<br>
Lägg till ny bok<br>
Ändra en boks information<br>
Ta bort en bok<br>

Bibliotekstjänsten har en bokmodell som innehåller id, titel. Håller bara koll på böckerna.

## En tjänst där användare kan låna böcker:

Låna bok (kollar om bok finns och om den är utlånad)<br>
Lämna tillbaka en bok<br>
Se alla sina lånade böcker<br>

Lånetjänsten har en modell som kopplar ihop användare med bok, innehåller användarens id och bokens id. Håller bara koll på vilka böcker en användare lånat.

## Endpoints

Alla endpoints testas via Postman.<br>
Alla enpoints försedda med \* kräver att JWT-token skickas med i headern.

### UserService

POST http://localhost:5000/register (kräver User-objekt i json format i body)<br>
POST http://localhost:5000/login (kräver UserLogin-objekt i json format i body)<br>
*GET http://localhost:5000/users<br>
*PUT http://localhost:5000/user/{userId}<br>
\*PUT http://localhost:5000/blacklist/{userId}<br>

### LibraryService

*POST http://localhost:5001/book (kräver Book-objekt i json format i body)<br>
*PUT http://localhost:5001/book/{bookId}<br>
\*DELETE http://localhost:5001/book/{bookId}<br>
GET http://localhost:5001/book/{bookId}<br>
GET http://localhost:5001/books<br>

### LoanService

*POST http://localhost:5002/loan/{bookId}<br>
*POST http://localhost:5002/return/{bookId}<br>
\*GET http://localhost:5002/loans<br>
