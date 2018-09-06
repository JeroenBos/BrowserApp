import { Component, Inject, OnInit, ViewChild } from '@angular/core';
import { Http } from '@angular/http';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';
import { Counter } from '../counter/counter.component';
import { BaseComponent, BaseViewModel, ICommandManager, CommandManagerId } from '../base.component';
import { CommandManager, CommandManagerViewModel } from '../../commands/commandManager';
import { CanonicalInputBinding } from '../../commands/inputBindingParser';
import { CommandBindingWithCommandName } from '../../commands/commands';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent extends BaseComponent<App> implements OnInit {
    @ViewChild(CommandManager) // @ts-ignore: setter is used implicitly
    private set commandManagerSetter(value: any) {
        (<any>this).commandManager = value;
    }
    public static readonly id: number = 0;
    public get counter() {
        return this.viewModel.counter;
    }

    constructor(http: Http,
        @Inject('BASE_URL') baseUrl: string) {
        super(new ChangesPropagator(http, baseUrl, () => this.createInitialViewModel()))

        this.server.open();
    }

    private createInitialViewModel(): App {
        const commandManager: CommandManagerViewModel = {
            __id: CommandManagerId,
            flags: new Map<string, boolean>(),
            inputBindings: new Map<CanonicalInputBinding, CommandBindingWithCommandName[]>(),
            commands: {}
        };

        const viewModel: App = {
            __id: AppComponent.id,
            commandManager: commandManager,
            counter: <any>undefined // prevents warning
        };
        (<any>this).viewModel = viewModel;
        return this.viewModel;
    }


    public ngOnInit() {
        //if (this.commandManager == null) {
        //    throw new Error(`The app component html is invalid: no command manager was created`);
        //}
    }
}
export interface App extends BaseViewModel {
    counter: Counter;
    commandManager: CommandManagerViewModel;
}

