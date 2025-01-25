# MetaExchange

A trading execution planner that calculates optimal buy & sell orders across multiple exchanges.

Includes both [API](#api) and [Console](#console) interfaces.

## Console

Run the console application with the following command:

```bash
dotnet run --project src/MetaExchange.Console <OrderBookFilePath>
```

## API

Set the configuration value for `OrderBookFilePath` in the `appsettings.Development.json` file.

Run the API with the following command:

```bash
dotnet run --project src/MetaExchange.Api
```

The API will be available at `http://localhost:5147`.

### Endpoints

#### `GET /api/v1/meta-exchange/plan`

Query parameters:

- `type` (required): `buy` or `sell`
- `amount` (required): the amount of BTC to buy or sell

Example:

```
GET /api/v1/meta-exchange/plan?type=buy&amount=1
```