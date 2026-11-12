# Laboratorul Î4 · Nechita Paula · nr. matricol 1212 · varianta A2 · B1

| Parametru | Valoarea mea |
|---|---|
| A (ultima cifră a nr. matricol: 2) | A2 |
| B (penultima cifră a nr. matricol: 1) | B1 |
| V1 · cărți pe pagină | 7 |
| V2 · sortarea implicită a catalogului | preț descrescător |
| V3 · filtrul din S3 | an de la–până la |
| V4 · indicatorul din S2 | prețul mediu al cărților autorului |

## S1 · Comenzile

- Protecția la overposting aleasă: [De completat]
- De ce am ales-o: [De completat]

## S2 · Pagina unui autor și sortarea după autor

[De completat după implementare]

## S3 · Filtrul variantei mele (V3)

[De completat după implementare]

## S4 · Folosirea AI

[De completat pe parcursul laboratorului]

## Ce nu am reușit (opțional)

[De completat la final]



## Laboratorul Î5

### A1 – Import evaluări
- Au fost importate 6599 de evaluări din book-ratings.csv.
- Importul a fost realizat din pagina Admin.

### A2 – Reantrenarea modelului
- Modelul ML.NET a fost reantrenat cu 6599 de evaluări.
- RMSE: [completează valoarea afișată în aplicație].

### A3 – API de recomandări
- Prag pentru varianta A2: 10 evaluări.
- Cititorul 5: API testat cu succes, 5 recomandări.
- Cititorul 999: API testat cu succes, 5 recomandări alternative.
- Recomandările includ ID-ul cărții, titlul și scorul.

### A4 – Clasificarea genurilor literare
- Fișier utilizat: book-descriptions.csv.
- Model antrenat cu ML.NET.
- MacroAccuracy: 0,7483 (74,83%).
- MicroAccuracy: 0,7509 (75,09%).

### S1 – Jurnalizarea recomandărilor
- Am creat tabelul RecommendationLog prin migrare EF Core.
- Recomandările personalizate și alternative sunt salvate automat.
- Pagina /RecommendationLogs afișează istoricul recomandărilor.
- Filtrarea după ID-ul cititorului a fost testată cu succes.
- Paginare implementată: 10 înregistrări pe pagină.


### S2 – Dashboard statistic
- Totalul cărților, evaluărilor și cititorilor este calculat din baza de date.
- Media evaluărilor este afișată în dashboard.
- Este afișat numărul recomandărilor înregistrate.
- Varianta B1: grafic cu numărul evaluărilor grupate după autor.
- Dashboard-ul a fost testat cu succes la /Admin.


## Laboratorul 9

### A1 – Portalul cititorului

Am creat aplicația Biblioteca.Portal, care comunică prin gRPC cu serviciul de recomandări din Intalnirea1.

Am testat cititorul cu ID 1212, solicitând 5 recomandări.

Rezultat: au fost afișate 5 cărți, toate cu eticheta „cititor nou”.

Eticheta apare deoarece cititorul nu are evaluări înregistrate, iar sistemul folosește recomandări bazate pe popularitatea cărților.


### A2 – Evoluția contractului gRPC

Am adăugat câmpul label = 5 în recommender.proto din server și am implementat etichetele pentru varianta A2.

Praguri:
- Foarte recomandată: scor >= 3,8
- Recomandată: scor >= 2,8

Test de compatibilitate:
Am pornit serverul actualizat și Biblioteca.Portal cu versiunea veche a fișierului recommender.proto.

Rezultat: portalul a afișat corect 5 recomandări pentru cititorul 1212, cu titluri, scoruri și indicatorul „cititor nou”.

Concluzie: adăugarea unui câmp nou în contractul Protocol Buffers nu afectează clientul existent, deoarece acesta ignoră câmpurile necunoscute.

#### Test Biblioteca.Client cu vechiul contract gRPC

Opțiunea 1 – Unary:
Au fost afișate 3 recomandări, cu titluri și scoruri, fără erori.

Opțiunea 2 – Server streaming:
Au fost afișate 5 recomandări succesiv, cu marcaje de timp și scoruri, fără erori.

Concluzie: Biblioteca.Client funcționează în continuare cu serverul actualizat, chiar dacă folosește versiunea veche a contractului recommender.proto.

#### Experiment – Modificarea numărului câmpului title

Am modificat temporar câmpul title din recommender.proto al serverului, de la 2 la 6, păstrând title = 2 în portal.

Rezultat:
- Coloana „Carte” a apărut goală pentru toate cele 5 recomandări.
- Scorurile și etichetele au continuat să fie afișate corect.

Concluzie:
Numerele câmpurilor din Protocol Buffers identifică datele transmise. Schimbarea numărului unui câmp existent afectează compatibilitatea cu clienții care folosesc vechiul contract.

După experiment, am revenit la title = 2 în server.
