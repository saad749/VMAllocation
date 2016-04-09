var menuStyle = {
    menu: 'dropdown-menu',
    menuSeparator: 'divider'
};

var markerContextMenuOptions = {
    classNames: menuStyle,
    menuItems: [
        {
            label: 'Delete', id: 'menu_delete',
            className: 'menu_item', eventName: 'menu_delete_clicked'
        },
        {
            label: 'Connect', id: 'menu_connect',
            className: 'menu_item', eventName: 'menu_connect_clicked'
        }
    ],
    pixelOffset: new google.maps.Point(10, -5),
    zIndex: 5
};

