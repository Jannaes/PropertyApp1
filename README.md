**PropertyApp - SmartUs**

PropertyApp -SmartUs on ASP.NET Core Razor Pages -pohjainen web-sovellus, jonka tarkoituksena on seurata kiinteistön tai asunnon mittauslaitteiden mittaustuloksia (kylmä vesi, lämmin vesi, sähkö- ja lämpömittarit).

Sovellus tukee useita käyttäjärooleja (owner / tenant) ja näyttää käyttäjälle vain ne tiedot, joihin hänellä on oikeus.

---

**Keskeiset ominaisuudet:**
- Oma kirjautumisjärjestelmä (ei ASP.NET Identity)
- Kiinteistöjen ja asuntojen hallinta
- Asukkaiden ja omistajien hallinta aikaväleittäin
- Mittauslaitteet (MeasureDevice)
- Mittaustulokset (Measures)
- Päivämääräkohtaiset mittaukset
- Kuvaaja (Chart) näyttää kunkin mittalaitteen mittaustulokset myös graafisesti
- Käyttäjä näkee vain omat mittauksensa (vuokrasuhteen voimassaolon perusteella)
- Responsiivinen käyttöliittymä (Bootstrap)

**Käytetyt teknologiat**
- ASP.NET Core Razor Pages
- Entity Framework Core
- SQL Server
- Bootstrap 5
- C#
- HTML / CSS

---

**Käyttäjäroolit:**
Owner -	Asunnon omistaja:
 - voi lisätä kiinteistöjä, asuntoja ja vuokralaisia
Tenant - Vuokralainen:
 - voi lisätä mittaustuloksia

**Mittausten näkyvyyslogiikka**

Käyttäjä näkee mittaustuloksen vain jos:
- hän on ollut asunnon omistaja tai vuokralainen
- mittauksen päivämäärä osuu hänen asumisjaksolleen
  
Tämä estää esim. aiempien asukkaiden mittausten näkymisen.

**Kirjautuminen**

Sovelluksessa käytetään itse toteutettua kirjautumista.

Kirjautunut käyttäjä tallennetaan Sessioniin:
HttpContext.Session.SetInt32("IdCurrentUser", user.IdUser);

Käyttäjän (omistajan) täytyy ensin rekisteröityä sovellukseen, jonka jälkeen hän voi lisätä kiinteistöjä ja asuntoja.
Vuokralainen kirjautuu palveluun asunnon omistajalta saamallaan käyttäjätunnuksella ja salasanalla.

---


**Sovelluksen käynnistäminen**

Projektissa käytetään SQL Server tietokantaa. 

- Kloonaa repositorio: git clone https://github.com/Jannaes/PropertyApp1.git

- Avaa projekti Visual Studiossa

- Luo tiedosto "appsettings.Development.json" projektin juureen ja lisää oma
tietokantayhteytesi:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PropertyApp;Trusted_Connection=True;"
  }
}

- Suorita migraatiot:

 Bash: dotnet ef database update

	tai

 Package Manager Console: Update-Database 

- Käynnistä sovellus (F5)

---

**Tekijät**
Projekti on toteutettu ryhmätyönä osana Tieto- ja viestintätekniikan perustutkinnon Ohjelmistokehittäjä-opintoja.

**Lisenssi**
Tämä projekti on tarkoitettu opetus- ja harjoituskäyttöön.
