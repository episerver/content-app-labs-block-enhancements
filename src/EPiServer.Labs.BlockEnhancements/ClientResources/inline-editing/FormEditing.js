define([
    "dojo/_base/declare",
    "dojo/topic",
    "epi-cms/contentediting/FormEditing"
], function (
    declare,
    topic,
    FormEditing
) {
    return declare([FormEditing], {
        _setViewModelAttr: function () {
            this.inherited(arguments);
            this.ownByKey("updateChangedProperty", topic.subscribe("updateChangedProperty", this._updateChangedProperty.bind(this)));
        },

        _updateChangedProperty: function () {
            this.viewModel.beginOperation();
            this.viewModel.setProperty("iversionable_changed", new Date(), null);
        }
    });
});
