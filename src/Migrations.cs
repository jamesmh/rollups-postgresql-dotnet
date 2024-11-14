using System.Data.Common;
using Dapper;

namespace RollupsPostgresqlDotnet
{
    public static class Migrations
    {
        public static async Task RunAsync(DbConnection connection)
        {
            await DropEverything(connection);
            await CreateTenantsTable(connection);
            await CreateUserEventsTable(connection);
            await GenerateUserEventsData(connection);

            await CreateRollupTable(connection);
        }

        private static async Task DropEverything(DbConnection connection)
        {
            await connection.ExecuteAsync(@"
                drop table if exists rollup_page_views_per_tenant_per_day;
                drop table if exists rollup_metadata;
                drop table if exists user_events;
                drop table if exists tenants;
            ");
        }

        private static async Task CreateTenantsTable(DbConnection connection)
        {
            await connection.ExecuteAsync(@"
                create table tenants (
                    id bigint primary key generated always as identity,
                    name text not null
                );

                insert into tenants (name) values ('1'), ('2'), ('3'), ('4'), ('5'), ('6');
            ");
        }

        private static async Task CreateUserEventsTable(DbConnection connection)
        {
            await connection.ExecuteAsync(@"
                create table user_events (
                    id bigint primary key generated always as identity,
                    event_type int,
                    tenant_id bigint references tenants (id) on delete cascade,
                    created_at timestamptz not null
                );

                create index user_events_dashboard_index on user_events using btree (tenant_id, event_type, created_at);
            ");
        }

        private static async Task GenerateUserEventsData(DbConnection connection)
        {

            // This will generate 1 million events.
            await connection.ExecuteAsync(@"
                create extension if not exists ""pgcrypto"";

                insert into user_events (event_type, tenant_id, created_at) 
                select 
                    -- Generate a random event type between 1 and 3
                    floor(random() * 3::integer + 1), 

                    -- Pick a tenant at random 
                    floor(random() * 6::integer + 1),

                     -- Generate 10 events per seconds starting from 2024-01-01
                    '2024-01-01 00:00:00.000'::timestamptz + concat(gen * .1, '1 second')::interval

                -- On a codespaces instance, this is enough data to prove the point.
                from generate_series(1, 1000000) gen;
            ", commandTimeout: 0); // E.g. this is going to take a while...

            // This will simulate that 1 tenant (6) has ALOT more data than other tenants.
            await connection.ExecuteAsync(@"
                insert into user_events (event_type, tenant_id, created_at) 
                select 
                    -- Imagine there are 3 event_types available 
                    floor(random() * 3::integer + 1), 

                    6, -- tenant_id

                     -- Generate 10 events per seconds starting from 2024-01-01
                    '2024-01-01 00:00:00.000'::timestamptz + concat(gen * .1, '1 second')::interval

                -- On a codespaces instance, this is enough data to prove the point.
                from generate_series(1, 5000000) gen;
            ", commandTimeout: 0); // E.g. this is going to take a while...
        }

        private static async Task CreateRollupTable(DbConnection connection)
        {
            await connection.ExecuteAsync(@"
                create table rollup_page_views_per_tenant_per_day (
                    id bigint primary key generated always as identity,
                    page_views bigint default(0),
                    tenant_id bigint references tenants (id) on delete cascade,
                    at_day timestamptz not null
                );

                create unique index rollup_page_views_per_tenant_per_day_unique_index on rollup_page_views_per_tenant_per_day (tenant_id, at_day);



                create table rollup_metadata (
                    id bigint primary key generated always as identity,
                    table_name text not null,
                    last_id bigint default(0)
                );

                insert into rollup_metadata (table_name, last_id) values ('rollup_page_views_per_tenant_per_day', 0);
            ");
        }
    }
}