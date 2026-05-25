(function () {
    const dtLanguageDe = {
        search: 'Suche:',
        lengthMenu: '_MENU_ Einträge',
        info: '_START_–_END_ von _TOTAL_',
        infoEmpty: 'Keine Einträge',
        infoFiltered: '(gefiltert aus _MAX_)',
        zeroRecords: 'Keine Treffer',
        paginate: { first: '«', last: '»', next: '›', previous: '‹' }
    };

    const dtLanguageEn = {
        search: 'Search:',
        lengthMenu: '_MENU_ entries',
        info: '_START_–_END_ of _TOTAL_',
        infoEmpty: 'No entries',
        infoFiltered: '(filtered from _MAX_)',
        zeroRecords: 'No matching records',
        paginate: { first: '«', last: '»', next: '›', previous: '‹' }
    };

    function getLanguage() {
        return document.documentElement.lang === 'en' ? dtLanguageEn : dtLanguageDe;
    }

    function adjustAllTables() {
        document.querySelectorAll('table.dataTable').forEach(function (table) {
            if ($.fn.DataTable.isDataTable(table)) {
                $(table).DataTable().columns.adjust();
            }
        });
    }

    function refreshWowheadTooltips() {
        if (window.$WowheadPower && typeof $WowheadPower.refreshLinks === 'function') {
            $WowheadPower.refreshLinks(true);
        } else if (window.WH && WH.Tooltip && typeof WH.Tooltip.refresh === 'function') {
            WH.Tooltip.refresh();
        }
    }

    function afterTableLayout() {
        adjustAllTables();
        refreshWowheadTooltips();
    }

    document.addEventListener('click', function (e) {
        const link = e.target.closest('a.item-link[data-item-detail-href]');
        if (!link || e.defaultPrevented) {
            return;
        }
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.altKey || e.button !== 0) {
            return;
        }
        e.preventDefault();
        window.location.href = link.getAttribute('data-item-detail-href');
    });

    window.initSoftresDataTable = function (selector, options) {
        const defaults = {
            pageLength: 100,
            lengthMenu: [[25, 50, 100, -1], [25, 50, 100, document.documentElement.lang === 'en' ? 'All' : 'Alle']],
            order: [],
            columnDefs: [
                { targets: '.col-item', className: 'col-item' },
                { targets: '.col-compact', className: 'col-compact' },
                { targets: '.col-nowrap', className: 'col-nowrap' }
            ],
            language: getLanguage()
        };

        const table = $(selector).DataTable(Object.assign({}, defaults, options || {}));

        setTimeout(afterTableLayout, 400);
        setTimeout(afterTableLayout, 1200);

        return table;
    };

    window.addEventListener('resize', afterTableLayout);
})();
