@{
    actions = @(
        @{type='design_decision';status='completed';description='Exclude scanner coverage from scores under explicit override, retain day-one outcome scope. Rejected treating the override as a waiver of platform/environment independence.'},
        @{type='design_decision';status='completed';description='Audit original per-test TRX data and compare Windows result names through authorized read-only mount after SSH SCP failed. Rejected relying on summary receipts alone.'},
        @{type='web_reference';status='completed';description='Microsoft Learn: REPARSE_DATA_BUFFER (ntifs.h). Used primary specification to check WindowsJunction header, mount-point fields, offsets and lengths. https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/ns-ntifs-_reparse_data_buffer'},
        @{type='validation';status='completed';filePath='docs/receipts/hv/hv-ug-r8-trx-all.json';description='Original TRX: Linux and Windows units each 4563/0/0; six hostile v6c runs each4563/0/0; lanes Linux433 Windows410, Build.Tests436. Name comparison removes none, adds exactly WindowsJunction binding. Own sandbox normal run4563/0/0. Reviewer hostile-max-1 failed restore before tests from read-only NuGet runtime storage; retry uses reviewer-owned XDG_RUNTIME_DIR and HTTP cache.'}
    )
}
