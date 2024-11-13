# rollups-postgresql-dotnet

Steps to run:

1. Run `docker-compose up` to run postgres

2. `cd ./src` then run `dotnet run`

3. There are two routes to test:
- `/home/PageViewsFor2024`
- `/home/PageViewsFor2024Rollup`

You'll be able to see the difference between a large events table being queried "raw" and a rollup table build specifically for that report/query.