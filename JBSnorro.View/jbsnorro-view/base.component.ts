import { Input, OnInit, HostListener } from './angular.index';
import { InputEvent } from './commands/inputTypes';
import { CommandInstruction } from './commands/commandInstruction';

export abstract class BaseComponent<TViewModel extends BaseViewModel> implements OnInit {
    // @ts-ignore: server has no initializer
    @Input() public readonly viewModel: TViewModel;
    // @ts-ignore: server has no initializer
    @Input() public readonly server: IChangePropagator;
    // @ts-ignore: server has no initializer
    @Input() public readonly commandManager: ICommandManager;

    public get __id(): number { return this.viewModel.__id; }
    public get isCollection(): boolean { return false; }
    public constructor(changesPropagator?: IChangePropagator, viewModel?: TViewModel) {
        this.server = <any>changesPropagator;
        if (viewModel !== undefined)
            this.viewModel = viewModel;
        if (Object.getPrototypeOf(this).constructor.name == "CommandManager")
            this.commandManager = <any>this;
    }

    public ngOnInit() {
        if (this.server == null) {
            throw new Error(`no changes propagator was specified for component '${Object.getPrototypeOf(this).constructor.name}'`);
        }
        if (this.viewModel == null) {
            throw new Error(`Forgot to bind viewModel property on component of type '${Object.getPrototypeOf(this).constructor.name}'`);
        }
        if (this.commandManager == null) {
            throw new Error(`no commandManager was specified for component '${Object.getPrototypeOf(this).constructor.name}'`);
        }
    }

    @HostListener('onkeydown', ['$event'])
    protected onKeyDown(e: KeyboardEvent): void {
        console.log("user pressed " + e.char);
        console.log(e);
    }
    @HostListener('click', ['$event'])
    protected onClick(e: MouseEvent) {
        this.commandManager.handleMouseClick(this, e);
    }
    @HostListener('mousemove', ['$event'])
    protected onMousemove(e: MouseEvent) {
        this.commandManager.handleMouseMove(this, e);
    }

    @HostListener('mousedown', ['$event'])
    protected onMousedown(e: MouseEvent) {
        this.commandManager.handleMouseDown(this, e);
    }
    @HostListener('mouseup', ['$event'])
    protected onMouseup(e: MouseEvent) {
        this.commandManager.handleMouseUp(this, e);
    }

}
export interface BaseViewModel {
    __id: number;
}

export const CommandManagerId = 2;

export interface ICommandManager extends BaseViewModel {
    handleMouseMove(sender: BaseViewModel, e: MouseEvent): void;
    handleMouseClick(sender: BaseViewModel, e: MouseEvent): void;
    handleMouseDown(sender: BaseViewModel, e: MouseEvent): void;
    handleMouseUp(sender: BaseViewModel, e: MouseEvent): void;
    handleKeypress(sender: BaseViewModel, e: KeyboardEvent): void;

    executeCommandByName(commandName: string, sender: BaseViewModel, e?: InputEvent): void;
}
export interface IChangePropagator {
    open(): void;
    executeCommand(commandInstruction: CommandInstruction): void;
}