SET NOCOUNT ON;
WAITFOR DELAY '00:00:01';
SELECT CAST(t.target_data AS nvarchar(max))
FROM sys.dm_xe_session_targets AS t
JOIN sys.dm_xe_sessions AS s ON t.event_session_address=s.address
WHERE s.name=N'qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f' AND t.target_name=N'ring_buffer';
ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER STATE=STOP;
IF EXISTS(SELECT 1 FROM sys.server_event_session_events AS e JOIN sys.server_event_sessions AS s ON s.event_session_id=e.event_session_id WHERE s.name=N'qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f' AND e.name=N'rpc_starting')
    ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER DROP EVENT sqlserver.rpc_starting;
IF EXISTS(SELECT 1 FROM sys.server_event_session_events AS e JOIN sys.server_event_sessions AS s ON s.event_session_id=e.event_session_id WHERE s.name=N'qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f' AND e.name=N'sql_batch_starting')
    ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER DROP EVENT sqlserver.sql_batch_starting;
ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER DROP TARGET package0.ring_buffer;
ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER ADD TARGET package0.ring_buffer(SET max_events_limit=100000,max_memory=65536);
ALTER EVENT SESSION [qbrain_migration_diag_c54571a0ab3e488283d5108ee4e6f75f] ON SERVER STATE=START;
