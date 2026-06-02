Design assumptions:
    1. Kafka retry topic should by default be consumed one by one. This is preffered since if we initially consume 
    & process events in batches then one failed event will send whole batch to retry. So the retry should be able to identify failed event and process correct ones. To avoid over writting
    3. Write repositories suppose to operate on domain entities (Which are also EF entities) and modify whole aggregates atomicaly in one transaction.
    4. Read repositorie should not operate on Domain entities. EF should register separate EF-Entities for reads


Rules: 
1. Projections tables should contain data same as on orgin service, so no mutations and no new artificial fields.
