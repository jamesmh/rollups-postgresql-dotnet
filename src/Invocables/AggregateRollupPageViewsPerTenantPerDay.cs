using System.Data.Common;
using Coravel.Invocable;
using Dapper;

namespace RollupsPostgresqlDotnet.Invocables;

public class AggregateRollupPageViewsPerTenantPerDay : IInvocable
{
    private readonly DbConnection connection;
    private static readonly int batchSize = 1_000_000;

    public AggregateRollupPageViewsPerTenantPerDay(DbConnection connection)
    {
        this.connection = connection;
    }

    public async Task Invoke()
    {
        var numProcessed = await connection.QuerySingleAsync<long?>(@"

            -- Grab the last id that this aggregation has processed.       
            with last_id as (
                select last_id as id 
                from rollup_metadata
                where table_name = 'rollup_page_views_per_tenant_per_day'
                limit 1
            ),

            -- Get the last id of the records that we will process right now.
            next_last_id as (
                select id 
                from user_events
                where id <= (select id + @batchSize from last_id)
                order by id desc
                limit 1
            ),

            -- This will perform the rollup aggregation for the next batch of records.
            do_aggregation as (
                insert into rollup_page_views_per_tenant_per_day (page_views, tenant_id, at_day)
                select 
                    count(*) as page_views,
                    tenant_id,
                    date_trunc('day', created_at) as at_day
                from user_events
                where 
                    event_type = 3
                    and id > (select id from last_id) and id <= (select id from next_last_id)
                group by tenant_id, at_day
                order by at_day asc

                on conflict (tenant_id, at_day) 
                    do update set 
                        page_views = rollup_page_views_per_tenant_per_day.page_views + excluded.page_views
                
                returning *
            ),

            -- Update the metadata table with the last id that we have processed.
            update_metadata_table as (            
                update rollup_metadata
                set last_id = (select id from next_last_id)
                where table_name = 'rollup_page_views_per_tenant_per_day'

                returning *
            )

            select (select id from next_last_id) - (select id from last_id) as num_processed
            
            ;
        ",  new {
            batchSize
        });

        Console.WriteLine($"#### Processed {numProcessed} records. #####");
    }
}