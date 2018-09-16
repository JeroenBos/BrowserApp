import { Component, Inject, OnInit, ViewChild, Http } from '../angular.index';
import { ChangesPropagator } from '../changesPropagator/ChangesPropagator';
import { BaseComponent, BaseViewModel, ICommandManager, CommandManagerId } from '../base.component';
import { CommandManager, CommandManagerViewModel } from '../commands/commandManager';
import { CanonicalInputBinding } from '../commands/inputBindingParser';
import { CommandBindingWithCommandName } from '../commands/commands';

@Component({
    selector: 'app.base',
    templateUrl: './app.base.component.html',
    styleUrls: ['./app.base.component.css']
})
export abstract class AppBaseComponent<TViewModel extends AppBase> extends BaseComponent<TViewModel>{
    @ViewChild(CommandManager) // @ts-ignore: setter is used implicitly
    private set commandManagerSetter(value: any) {
        (<any>this).commandManager = value;
    }
    public static readonly id: number = 0;

    constructor(http: Http,
        @Inject('BASE_URL') baseUrl: string) {
        super(new ChangesPropagator(http, baseUrl, () => this.createInitialViewModel()))

        this.server.open();
    }

    private createInitialViewModel(): AppBase {
        const commandManager: CommandManagerViewModel = {
            __id: CommandManagerId,
            flags: new Map<string, boolean>(),
            inputBindings: new Map<CanonicalInputBinding, CommandBindingWithCommandName[]>(),
            commands: {}
        };

        const viewModel: AppBase = {
            __id: AppBaseComponent.id,
            commandManager: commandManager,
        };
        (<any>this).viewModel = viewModel;
        return this.viewModel;
    }
    protected abstract populateInitialViewModel(appBaseViewModel: AppBase): void;
}
export interface AppBase extends BaseViewModel {
    commandManager: CommandManagerViewModel;
}

