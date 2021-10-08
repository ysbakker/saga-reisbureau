# Saga Reisbureau

Een project om het gebruik van een choreography-based saga te demonstreren.

![](.img/SagaImplementation.drawio.svg)

## Uitvoeren

Je kan het project uitvoeren door deze commando's in de rootmap uit te voeren:

```bash
docker-compose up --build -d
docker-compose logs -f ticketservice huurautoservice hotelservice reisapi
```