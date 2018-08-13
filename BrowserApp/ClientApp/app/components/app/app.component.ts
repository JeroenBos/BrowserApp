import { Component, Inject, ComponentFactory, OnInit, PACKAGE_ROOT_URL, AfterViewInit } from '@angular/core';
import { Http } from '@angular/http';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';
import { CommandManager } from '../../commands/commands';
import { Counter } from '../counter/counter.component';
import { BaseComponent, BaseViewModel } from '../base.component';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent extends BaseComponent<App> {
    public get counter() { return this.viewModel.counter; }

    constructor(http: Http,
        @Inject('BASE_URL') baseUrl: string) {
        super(new ChangesPropagator(http, baseUrl))

        this.server.open();
    }
}
export interface App extends BaseViewModel {
    counter: Counter;
    commandManager: CommandManager;
}

