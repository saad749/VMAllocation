var menuStyle = {
    menu: 'dropdown-menu',
    menuSeparator: 'divider'
};

var mapContextMenuOptions = {
    classNames: menuStyle,
    menuItems: [
        {
            label: 'option1', id: 'menu_option1',
            className: 'menu_item', eventName: 'option1_clicked'
        },
        {
            label: 'option2', id: 'menu_option2',
            className: 'menu_item', eventName: 'option2_clicked'
        }
    ],
    pixelOffset: new google.maps.Point(10, -5),
    zIndex: 5
};


