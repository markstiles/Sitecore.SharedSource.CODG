jQuery.noConflict();

//generate
jQuery(document).ready(function () {

    var importForm = ".import-form";

    jQuery('h2').dblclick(function () {
        $(this).next(".Section").toggle();
    });
    
    //generate
    var timer;
    jQuery(importForm + " .import-submit")
        .click(function (event) {
            event.preventDefault();

            var idValue = jQuery(importForm + " #id").val();
            var dbValue = jQuery(importForm + " #db").val();
            var importTypeValue = jQuery(this).attr("rel");

            jQuery(".importError").hide();
            jQuery(".importStatus").hide();
            jQuery(".form-buttons").hide();
            jQuery(".progress-indicator").show();

            jQuery.post(jQuery(importForm).attr("generate-action"),
                {
                    id: idValue,
                    db: dbValue,
                    importType: importTypeValue
                })
                .done(function (r) {
                    if (r.Failed) {
                        jQuery(".importError").show();
                        jQuery(".importError").text(r.Error);
                        jQuery(".importStatus").hide();
                        jQuery(".form-buttons").show();
                        jQuery(".progress-indicator").hide();

                        return;
                    }

                    var timer = setInterval(function () {
                        jQuery.post(jQuery(importForm).attr("status-action"),
                            {
                                handleName: r.HandleName
                            })
                            .done(function (jobResult) {
                                if (jobResult.Total < 0)
                                    return;

                                jQuery(".progress-indicator").hide();
                                jQuery(".importStatus").show();
                                jQuery(".status-message").text(jobResult.Message);
                                jQuery(".status-number").text(numberWithCommas(jobResult.Current) + " of " + numberWithCommas(jobResult.Total));
                                var percent = jobResult.Current / jobResult.Total * 100;
                                jQuery(".status-bar-color").attr("style", "width:" + percent + "%;");

                                if (jobResult.Completed) {
                                    jQuery(".form-buttons").show();
                                    clearInterval(timer);
                                }
                            });
                    }, 500);
                })
                .fail(function () {
                    jQuery(".importError").show();
                    jQuery(".importError").text(r.Error);
                    jQuery(".importStatus").hide();
                    jQuery(".form-buttons").show();
                    jQuery(".progress-indicator").hide();

                    return;
                });
        });

    function numberWithCommas(x) {
        return x.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
    }
});
