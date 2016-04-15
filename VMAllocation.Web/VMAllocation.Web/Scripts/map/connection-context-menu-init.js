var menuStyle = {
    menu: 'dropdown-menu',
    menuSeparator: 'divider'
};

var connectionContextMenuOptions = {
    classNames: menuStyle,
    menuItems: [
        {
            label: 'Delete', id: 'connection_delete',
            className: 'menu_item', eventName: 'connection_delete_clicked'
        }
    ],
    pixelOffset: new google.maps.Point(10, -5),
    zIndex: 5
};

